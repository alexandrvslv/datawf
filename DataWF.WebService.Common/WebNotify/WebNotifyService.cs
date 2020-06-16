using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DataWF.WebService.Common
{
    public class WebNotifyService : NotifyService, IWebNotifyService
    {
        protected readonly SelectableList<WebNotifyConnection> connections = new SelectableList<WebNotifyConnection>();

        public static WebNotifyService Instance { get; private set; }

        public event EventHandler<WebNotifyEventArgs> ReceiveMessage;
        public event EventHandler<WebNotifyEventArgs> RemoveConnection;

        public WebNotifyService()
        {
            Instance = this;
            connections.Indexes.Add(WebNotifyConnectionUserInvoker.Instance.Name,
                new ListIndex<WebNotifyConnection, IUserIdentity>(
                    WebNotifyConnectionUserInvoker.Instance,
                    NullUser.Value));
        }

        public WebNotifyConnection GetBySocket(WebSocket socket)
        {
            return connections.SelectOne(nameof(WebNotifyConnection.Socket), socket);
        }

        public IEnumerable<WebNotifyConnection> GetByUser(IUserIdentity user)
        {
            return connections.Select(WebNotifyConnectionUserInvoker.Instance, CompareType.Equal, user);
        }

        public void SetCurrentAction(ActionExecutingContext context)
        {
            var user = context.HttpContext.User?.GetCommonUser();
            SetCurrentAction(user, context);
        }

        public void SetCurrentAction(IUserIdentity user, ActionExecutingContext context)
        {
            SetCurrentAction(user, context.HttpContext.Connection.RemoteIpAddress.ToString(), context.ActionDescriptor.DisplayName);
        }

        public void SetCurrentAction(IUserIdentity user, string address, string action)
        {
            if (user != null)
            {
                foreach (var connection in GetByUser(user).Where(p => p.Address.Equals(address, StringComparison.Ordinal)))
                {
                    connection.Action = action;
                }
            }
            Helper.Logs.Add(new StateInfo("Web Request", action, address) { User = user?.Name });
        }

        public virtual WebNotifyConnection Register(WebSocket socket, IUserIdentity user, string address)
        {
            var connection = GetBySocket(socket);
            if (connection == null)
            {
                connection = new WebNotifyConnection
                {
                    Socket = socket,
                    User = user,
                    Address = address,
                };
                connections.Add(connection);
            }
            return connection;
        }

        public IEnumerable<WebNotifyConnection> GetConnections()
        {
            return connections;
        }

        public async void CloseAsync(DBUser user)
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

        public async Task CloseAsync(WebNotifyConnection connection)
        {
            if (connection != null)
            {
                await connection.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Internal Server Close.", CancellationToken.None);
                Remove(connection);
            }
        }

        public virtual bool Remove(WebNotifyConnection connection)
        {
            var removed = false;
            try
            {
                if ((removed = connections.Remove(connection)))
                {
                    Debug.WriteLine($"Remove webSocket from {connection.UserEmail}");
                    connection?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
            }

            RemoveConnection?.Invoke(this, new WebNotifyEventArgs(connection));
            return removed;
        }

        //https://github.com/radu-matei/websocket-manager/blob/blog-article/src/WebSocketManager/WebSocketManagerMiddleware.cs
        public async Task ListenAsync(WebNotifyConnection connection)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            while (CheckConnection(connection))
            {
                try
                {
                    WebSocketReceiveResult result = null;
                    var stream = new MemoryStream();
                    do
                    {
                        result = await connection.Socket.ReceiveAsync(buffer, CancellationToken.None);
                        if (result.Count > 0)
                        {
                            stream.Write(buffer.Array, buffer.Offset, result.Count);
                        }
                    }
                    while (!result.EndOfMessage);

                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Binary:
                        case WebSocketMessageType.Text:
                            _ = OnMessageReceive(connection, stream);
                            break;
                        case WebSocketMessageType.Close:
                            stream.Dispose();
                            await connection.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good luck!", CancellationToken.None);
                            Remove(connection);
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            }
            Remove(connection);
        }

        protected virtual async Task<object> OnMessageReceive(WebNotifyConnection connection, MemoryStream stream)
        {
            await Task.Delay(10).ConfigureAwait(false);

            object obj = LoadMessage(connection, stream);
            if (obj is WebNotifyRegistration registration)
            {
                connection.Platform = registration.Platform;
                connection.Application = registration.Application;
                connection.Version = registration.Version;
                connection.VersionValue = Version.TryParse(connection.Version, out var version) ? version : new Version("1.0.0.0");
            }
            ReceiveMessage?.Invoke(this, new WebNotifyEventArgs(connection, obj));
            return obj;
        }

        protected object LoadMessage(WebNotifyConnection connection, MemoryStream stream)
        {
            stream.Position = 0;
            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.InitDefaults(new DBItemConverterFactory { CurrentUser = connection.User });
            var span = new ReadOnlySpan<byte>(stream.GetBuffer(), 0, (int)stream.Length);
            var property = (string)null;
            var type = (Type)null;
            var obj = (object)null;
            var jreader = new Utf8JsonReader(span);
            {
                while (jreader.Read())
                {
                    switch (jreader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            property = jreader.GetString();
                            break;
                        case JsonTokenType.String:
                            switch (property)
                            {
                                case ("Type"):
                                    type = TypeHelper.ParseType(jreader.GetString());
                                    break;
                            }
                            break;
                        case JsonTokenType.StartObject:
                            if (string.Equals(property, "Value", StringComparison.Ordinal) && type != null)
                            {
                                property = null;
                                obj = JsonSerializer.Deserialize(ref jreader, type, jsonOptions);
                            }
                            break;
                    }
                }
            }
#if DEBUG
            Debug.WriteLine($"Receive Message {DateTime.UtcNow} Length: {stream.Length} ");
#endif
            stream.Dispose();
            return obj;
        }

        protected override async void OnSendChanges(NotifyMessageItem[] list)
        {
            if (list == null)
            {
                return;
            }
            try
            {
                base.OnSendChanges(list);
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
            }
            await SendToWebClients(list);
        }

        private async Task SendToWebClients(NotifyMessageItem[] list)
        {
            foreach (var connection in connections.ToList())
            {
                try
                {
                    if (!CheckConnection(connection))
                    {
                        Remove(connection);
                        continue;
                    }
                    using (var stream = WriteData(list, connection.User))
                    {
                        if (stream == null)
                            continue;
                        await SendStream(connection, stream);
                    }
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            }
        }

        public bool CheckConnection(WebNotifyConnection connection)
        {
            return connection?.Socket != null
                && (connection.State == WebSocketState.Open
                || connection.State == WebSocketState.Connecting);
        }

        public async Task SendText(WebNotifyConnection connection, string text)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(text);
            using (var stream = new MemoryStream(buffer))
            {
                await SendStream(connection, stream);
            }
        }

        public async Task SendStream(WebNotifyConnection connection, Stream stream)
        {
            stream.Position = 0;
            var bufferLength = 8 * 1024;
            var buffer = new byte[bufferLength];
            var count = 0;
            while ((count = stream.Read(buffer, 0, bufferLength)) > 0)
            {
                await connection.Socket.SendAsync(new ArraySegment<byte>(buffer, 0, count)
                    , WebSocketMessageType.Binary
                    , stream.Position == stream.Length
                    , CancellationToken.None);
            }
#if DEBUG
            Debug.WriteLine($"Send Message {DateTime.UtcNow} Length: {stream.Length} ");
#endif
        }

        public async Task SendObject(WebNotifyConnection connection, object data)
        {
            using (var stream = WriteData(data, connection.User))
            {
                await SendStream(connection, stream);
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

        protected MemoryStream WriteData(object data, IUserIdentity user)
        {
            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.InitDefaults(new DBItemConverterFactory
            {
                CurrentUser = user,
                HttpJsonSettings = HttpJsonSettings.None,
            });
            var stream = new MemoryStream();

            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Encoder = jsonOptions.Encoder,
                Indented = jsonOptions.WriteIndented
            }))
            {
                writer.WriteStartObject();
                writer.WriteString("Type", data.GetType().Name);
                writer.WritePropertyName("Value");
                JsonSerializer.Serialize(writer, data, data.GetType(), jsonOptions);
                writer.WriteEndObject();
                writer.Flush();
            }
            return stream;
        }

        private MemoryStream WriteData(NotifyMessageItem[] list, IUserIdentity user)
        {
            bool haveValue = false;
            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.InitDefaults(new DBItemConverterFactory
            {
                CurrentUser = user,
                HttpJsonSettings = HttpJsonSettings.None,
            });
            var stream = new MemoryStream();

            //using (var streamWriter = new StreamWriter(stream, Encoding.UTF8, 80 * 1024, true))
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Encoder = jsonOptions.Encoder,
                Indented = jsonOptions.WriteIndented
            }))
            {
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
                        writer.WriteString("Type", itemType.Name);
                        writer.WritePropertyName("Items");
                        writer.WriteStartArray();
                    }
                    if (!item.ItemId.Equals(id)
                        && (item.UserId != user.Id || item.Type == DBLogType.Delete))
                    {
                        id = item.ItemId;
                        writer.WriteStartObject();
                        writer.WriteNumber("Diff", (int)item.Type);
                        writer.WriteNumber("User", item.UserId);
                        writer.WriteString("Id", item.ItemId.ToString());
                        if (item.Type != DBLogType.Delete)
                        {
                            var value = item.Table.LoadItemById(item.ItemId);
                            if (value != null
                                && (value.Access?.GetFlag(AccessType.Read, user) ?? false)
                                && value.PrimaryId != null)
                            {
                                writer.WritePropertyName("Value");
                                JsonSerializer.Serialize(writer, value, value?.GetType(), jsonOptions);
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
                stream = null;
            }
            else
            {
                stream.Position = 0;
            }
            return stream;
        }
    }
}
