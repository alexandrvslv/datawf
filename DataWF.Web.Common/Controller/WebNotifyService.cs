using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Web.Common
{
    public class WebNotifyService : NotifyService
    {
        private SelectableList<WebNotifyConnection> connections = new SelectableList<WebNotifyConnection>();
        private JsonSerializerSettings jsonSettings;

        public static WebNotifyService Instance { get; private set; }

        public event EventHandler<WebNotifyEventArgs> ReceiveMessage;
        public event EventHandler<WebNotifyEventArgs> RemoveClient;

        public WebNotifyService()
        {
            Instance = this;
            connections.Indexes.Add(WebNotifyConnection.SocketInvoker);
            connections.Indexes.Add(WebNotifyConnection.UserInvoker);
            jsonSettings = new JsonSerializerSettings { ContractResolver = DBItemContractResolver.Instance };
            jsonSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        }

        public WebNotifyConnection GetBySocket(WebSocket socket)
        {
            return connections.SelectOne(WebNotifyConnection.SocketInvoker.Name, socket);
        }

        public IEnumerable<WebNotifyConnection> GetByUser(User user)
        {
            return connections.Select(WebNotifyConnection.UserInvoker, CompareType.Equal, user);
        }

        public void SetCurrentAction(AuthorizationFilterContext context)
        {
            var emailClaim = context.HttpContext.User?.FindFirst(ClaimTypes.Email);
            var user = emailClaim != null ? User.GetByEmail(emailClaim.Value) : null;
            SetCurrentAction(user, context);
        }

        public void SetCurrentAction(User user, AuthorizationFilterContext context)
        {
            SetCurrentAction(user, context.HttpContext.Connection.RemoteIpAddress.ToString(), context.ActionDescriptor.DisplayName);
        }

        public void SetCurrentAction(User user, string address, string action)
        {
            if (user != null)
            {
                foreach (var connection in GetByUser(user).Where(p => p.Address.Equals(address, StringComparison.Ordinal)))
                {
                    connection.Action = action;
                }
            }
            Helper.Logs.Add(new StateInfo("Web Request", action, address) { User = user?.EMail });
        }

        public void Register(WebSocket socket, User user, string address)
        {
            var client = GetBySocket(socket);
            if (client == null)
            {
                client = new WebNotifyConnection
                {
                    Socket = socket,
                    User = user,
                    Address = address
                };
                connections.Add(client);
            }
        }

        public IEnumerable<WebNotifyConnection> GetConnections()
        {
            return connections;
        }

        public async void CloseAsync(User user)
        {
            foreach (var item in GetByUser(user))
            {
                await CloseAsync(item);
            }
        }

        public async void CloseAsync(WebSocket socket)
        {
            await CloseAsync(GetBySocket(socket));
        }

        public async Task CloseAsync(WebNotifyConnection client)
        {
            if (client != null)
            {
                await client.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Internal Server Close.", CancellationToken.None);
                Remove(client);
            }
        }

        private void Remove(WebNotifyConnection client)
        {
            try
            {
                lock (client)
                {
                    if (connections.Contains(client))
                    {
                        connections.Remove(client);
                        client.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
            }

            RemoveClient?.Invoke(this, new WebNotifyEventArgs(client));
        }

        //https://github.com/radu-matei/websocket-manager/blob/blog-article/src/WebSocketManager/WebSocketManagerMiddleware.cs
        public async Task ListenAsync(WebSocket socket)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            var client = GetBySocket(socket);
            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult result = null;
                    using (var stream = new MemoryStream())
                    {
                        do
                        {
                            result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                            if (result.Count > 0)
                            {
                                stream.Write(buffer.Array, buffer.Offset, result.Count);
                            }
                        }
                        while (!result.EndOfMessage);

                        switch (result.MessageType)
                        {
                            case WebSocketMessageType.Text:
                                var message = Encoding.UTF8.GetString(stream.ToArray());
                                ReceiveMessage?.Invoke(this, new WebNotifyEventArgs(client, message));
                                break;
                            case WebSocketMessageType.Close:
                                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good luck!", CancellationToken.None);
                                Remove(client);
                                return;
                        }
                    }

                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    Remove(client);
                }

            }
        }

        protected override async void OnSendChanges(NotifyMessageItem[] list)
        {
            if (list == null)
            {
                return;
            }

            base.OnSendChanges(list);
            await SendToWebClients(list);
        }

        private async Task SendToWebClients(NotifyMessageItem[] list)
        {
            CheckConnections();
            foreach (var connection in connections)
            {
                try
                {
                    var buffer = new byte[8 * 1024];
                    using (var stream = WriteData(list, connection.User))
                    {
                        if (stream == null)
                            return;
                        while (stream.Position < stream.Length)
                        {
                            var count = stream.Read(buffer, 0, buffer.Length);

                            await connection.Socket.SendAsync(new ArraySegment<byte>(buffer, 0, count)
                                , WebSocketMessageType.Text
                                , stream.Position == stream.Length
                                , CancellationToken.None);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            }
        }

        private void CheckConnections()
        {
            foreach (var connection in connections)
            {
                try
                {
                    if (connection.State != WebSocketState.Open
                        && connection.State != WebSocketState.Connecting)
                    {
                        Remove(connection);
                    }
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    Remove(connection);
                }
            }
        }

        protected override async void OnMessageLoad(EndPointMessage message)
        {
            base.OnMessageLoad(message);
            if (message.Type == SocketMessageType.Data)
            {
                var list = ParseMessage(message.Data);
                if (list.Length > 0)
                {
                    await SendToWebClients(list);
                }
            }
        }

        private NotifyMessageItem[] ParseMessage(byte[] data)
        {
            var list = new List<NotifyMessageItem>();
            var stream = new MemoryStream(data);
            using (var reader = new BinaryReader(stream))
            {
                while (reader.PeekChar() == 1)
                {
                    reader.ReadChar();
                    var tableName = reader.ReadString();
                    var table = DBService.Schems.ParseTable(tableName);

                    while (reader.PeekChar() == 2)
                    {
                        reader.ReadChar();
                        var item = new NotifyMessageItem
                        {
                            Table = table,
                            Type = (DBLogType)reader.ReadInt32(),
                            UserId = reader.ReadInt32(),
                            ItemId = Helper.ReadBinary(reader),
                        };
                        if (table != null)
                        {
                            list.Add(item);
                        }
                    }
                }
            }
            return list.ToArray();
        }

        private MemoryStream WriteData(NotifyMessageItem[] list, User user)
        {
            bool haveValue = false;
            var stream = new MemoryStream();
            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8, 80 * 1024, true))
            using (var writer = new ClaimsJsonTextWriter(streamWriter)
            {
                User = user,
                IncludeReferences = false,
                IncludeReferencing = false,
                CloseOutput = false
            })
            {
                var jsonSerializer = JsonSerializer.Create(jsonSettings);
                writer.WriteStartArray();
                Type itemType = null;
                object id = null;
                foreach (var item in list)
                {
                    if (item.Table.ItemType.Type != itemType)
                    {
                        if (itemType != null)
                        {
                            writer.WriteEndArray();
                            writer.WriteEndObject();
                        }
                        itemType = item.Table.ItemType.Type;
                        writer.WriteStartObject();
                        writer.WritePropertyName("Type");
                        writer.WriteValue(itemType.Name);
                        writer.WritePropertyName("Items");
                        writer.WriteStartArray();
                    }
                    if (!item.ItemId.Equals(id)
                        && (item.UserId != user.Id || item.Type == DBLogType.Delete))
                    {
                        id = item.ItemId;
                        writer.WriteStartObject();
                        writer.WritePropertyName("Diff");
                        writer.WriteValue((int)item.Type);
                        writer.WritePropertyName("User");
                        writer.WriteValue(item.UserId);
                        writer.WritePropertyName("Id");
                        writer.WriteValue(item.ItemId.ToString());
                        if (item.Type != DBLogType.Delete)
                        {
                            var value = item.Table.LoadItemById(item.ItemId);
                            if (value?.Access?.GetFlag(AccessType.Read, user) ?? false
                                && value.PrimaryId != null)
                            {
                                writer.WritePropertyName("Value");
                                jsonSerializer.Serialize(writer, value, value?.GetType());
                                haveValue = true;
                            }
                        }
                        else
                        {
                            haveValue = true;
                        }
                        writer.WriteEndObject();
                    }
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.WriteEndArray();
                writer.Flush();

            }
            if (!haveValue)
            {
                stream.Dispose();
            }
            else
            {
                stream.Position = 0;
            }
            return stream;
        }
    }

    public class WebNotifyEventArgs : EventArgs
    {
        public WebNotifyEventArgs(WebNotifyConnection client, string message = null)
        {
            Client = client;
            Message = message;
        }

        public WebNotifyConnection Client { get; set; }
        public string Message { get; set; }
    }
}
