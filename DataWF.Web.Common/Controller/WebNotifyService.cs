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
        private SelectableList<WebNotifyClient> Clients = new SelectableList<WebNotifyClient>();

        public event EventHandler<WebNotifyEventArgs> ReceiveMessage;
        public event EventHandler<WebNotifyEventArgs> RemoveClient;

        public void WebSocketrequest()
        {
            Clients.Indexes.Add(WebNotifyClient.SocketInvoker);
        }

        public WebNotifyClient GetBySocket(WebSocket socket)
        {
            return Clients.SelectOne(WebNotifyClient.SocketInvoker.Name, socket);
        }

        public WebNotifyClient GetByUser(User user)
        {
            return Clients.SelectOne(WebNotifyClient.UserInvoker.Name, user);
        }

        public void Register(WebSocket socket, User user)
        {
            var client = GetBySocket(socket);
            if (client == null)
            {
                client = new WebNotifyClient { Socket = socket, User = user };
                Clients.Add(client);
            }
        }

        public async void CloseAsync(User user)
        {
            await CloseAsync(GetByUser(user));
        }

        public async void CloseAsync(WebSocket socket)
        {
            await CloseAsync(GetBySocket(socket));
        }

        public async Task CloseAsync(WebNotifyClient client)
        {
            if (client != null)
            {
                Clients.Remove(client);
                await client.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Internal Server Close.", CancellationToken.None);
                client.Dispose();
            }
        }

        private void Remove(WebNotifyClient client)
        {
            Clients.Remove(client);
            RemoveClient?.Invoke(this, new WebNotifyEventArgs(client));
            client.Dispose();
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
            await SendToWebClient(list);
        }

        private async Task SendToWebClient(NotifyMessageItem[] list)
        {
            var buffer = new ArraySegment<byte>(WriteData(list));
            foreach (var client in Clients)
            {
                if (client.Socket.State != WebSocketState.Open)
                {
                    continue;
                }
                try
                {
                    await client.Socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
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
                    await SendToWebClient(list);
                }
            }
        }

        private static NotifyMessageItem[] ParseMessage(byte[] data)
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

        private static byte[] WriteData(NotifyMessageItem[] list)
        {
            using (var stream = new MemoryStream())
            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8))
            using (var writer = new JsonTextWriter(streamWriter))
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
                        writer.WritePropertyName("Type");
                        writer.WriteValue(itemType.Name);
                        writer.WritePropertyName("Items");
                        writer.WriteStartArray();
                    }
                    if (!item.ItemId.Equals(id))
                    {
                        id = item.ItemId;
                        writer.WriteStartObject();
                        writer.WritePropertyName("Diff");
                        writer.WriteValue((int)item.Type);
                        writer.WritePropertyName("User");
                        writer.WriteValue(item.UserId);
                        writer.WritePropertyName("Id");
                        writer.WriteValue(item.ItemId.ToString());
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
        public WebNotifyEventArgs(WebNotifyClient client, string message = null)
        {
            Client = client;
            Message = message;
        }

        public WebNotifyClient Client { get; set; }
        public string Message { get; set; }
    }
}
