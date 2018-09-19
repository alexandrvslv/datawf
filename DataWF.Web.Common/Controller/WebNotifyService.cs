using DataWF.Common;
using DataWF.Module.Common;
using Newtonsoft.Json;
using System;
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

        public void Register(WebSocket socket)
        {
            var client = GetBySocket(socket);
            if (client == null)
            {
                client = new WebNotifyClient { Socket = socket, User = User.CurrentUser };
                Clients.Add(client);
            }
        }

        public async void Close(User user)
        {
            await Close(GetByUser(user));
        }

        public async void Close(WebSocket socket)
        {
            await Close(GetBySocket(socket));
        }

        public async Task Close(WebNotifyClient client)
        {
            if (client != null)
            {
                Clients.Remove(client);
                await client.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Internal Server Close.", CancellationToken.None);
            }
        }

        protected override async void OnSendChanges(NotifyMessageItem[] list)
        {
            base.OnSendChanges(list);
            var buffer = new ArraySegment<byte>(WriteData(list));
            foreach (var client in Clients)
            {
                if (client.Socket.State != WebSocketState.Open)
                {
                    continue;
                }
                await client.Socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private void Remove(WebNotifyClient client)
        {
            Clients.Remove(client);
            RemoveClient?.Invoke(this, new WebNotifyEventArgs(client));
        }

        private static byte[] WriteData(NotifyMessageItem[] list)
        {
            using (var stream = new MemoryStream())
            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8))
            using (var writer = new JsonTextWriter(streamWriter))
            {
                writer.WriteStartArray();
                Type itemType = null;
                foreach (var item in list)
                {
                    if (item.Item.GetType() != itemType)
                    {
                        if (itemType != null)
                        {
                            writer.WriteEndArray();
                            writer.WriteEndObject();
                        }
                        itemType = item.Item.GetType();
                        writer.WriteStartObject();
                        writer.WritePropertyName("Type");
                        writer.WriteValue(itemType.Name);
                        writer.WritePropertyName("Items");
                        writer.WriteStartArray();
                    }
                    writer.WriteStartObject();
                    writer.WritePropertyName("Id");
                    writer.WriteValue(item.Item.PrimaryId.ToString());
                    writer.WritePropertyName("Diff");
                    writer.WriteValue((int)item.Type);
                    writer.WritePropertyName("User");
                    writer.WriteValue(item.UserId);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.WriteEndArray();
                writer.Flush();
                return stream.ToArray();
            }
        }

        //https://github.com/radu-matei/websocket-manager/blob/blog-article/src/WebSocketManager/WebSocketManagerMiddleware.cs
        public async Task Receive(WebSocket socket)
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
