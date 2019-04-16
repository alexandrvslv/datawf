using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Web.Common
{
    public class WebNotifyService : NotifyService
    {
        private SelectableList<WebNotifyConnection> connections = new SelectableList<WebNotifyConnection>();
        private JsonSerializerSettings jsonSettings;

        public event EventHandler<WebNotifyEventArgs> ReceiveMessage;
        public event EventHandler<WebNotifyEventArgs> RemoveClient;

        public WebNotifyService()
        {
            connections.Indexes.Add(WebNotifyConnection.SocketInvoker);
            jsonSettings = new JsonSerializerSettings { ContractResolver = DBItemContractResolver.Instance };
            jsonSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

        }

        public WebNotifyConnection GetBySocket(WebSocket socket)
        {
            return connections.SelectOne(WebNotifyConnection.SocketInvoker.Name, socket);
        }

        public IEnumerable<WebNotifyConnection> GetByUser(User user)
        {
            return connections.Select(WebNotifyConnection.UserInvoker.Name, CompareType.Equal, user);
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
                    await connection.Socket.SendAsync(new ArraySegment<byte>(WriteData(list, connection.User))
                        , WebSocketMessageType.Text
                        , true
                        , CancellationToken.None);
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
                    if (connection.State != WebSocketState.Open)
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

        private byte[] WriteData(NotifyMessageItem[] list, User user)
        {
            using (var stream = new MemoryStream())
            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8))
            using (var writer = new ClaimsJsonTextWriter(streamWriter)
            {
                User = user,
                SerializeReferencing = false
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
                    if (!item.ItemId.Equals(id) && (item.UserId != user.Id || item.Type == DBLogType.Delete))
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
                            if (value?.Access?.GetFlag(AccessType.Read, user) ?? false)
                            {
                                writer.WritePropertyName("Value");
                                jsonSerializer.Serialize(writer, value, value?.GetType());
                            }
                        }
                        writer.WriteEndObject();
                    }
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.WriteEndArray();
                writer.Flush();
                return stream.ToArray();
            }
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
