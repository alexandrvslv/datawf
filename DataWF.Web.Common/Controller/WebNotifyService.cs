using DataWF.Common;
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
            var buffer = WriteData(list);
            var toDelete = new List<WebNotifyClient>();
            foreach (var client in Clients)
            {
                if (client.Socket.CloseStatus != null)
                {
                    toDelete.Add(client);
                    continue;
                }
                await client.Socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            Clients.RemoveRange(toDelete);
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
            var buffer = new byte[1024 * 4];
            var client = GetBySocket(socket);
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                                                       cancellationToken: CancellationToken.None);

                switch (result.MessageType)
                {
                    case WebSocketMessageType.Text:
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        ReceiveMessage?.Invoke(this, new WebNotifyEventArgs(client, message));
                        break;
                    case WebSocketMessageType.Close:
                        Clients.Remove(GetBySocket(socket));
                        return;
                }
            }
        }
    }

    public class WebNotifyEventArgs : EventArgs
    {
        private WebNotifyClient client;
        private string message;

        public WebNotifyEventArgs(WebNotifyClient client, string message)
        {
            this.Client = client;
            this.Message = message;
        }

        public WebNotifyClient Client { get => client; set => client = value; }
        public string Message { get => message; set => message = value; }
    }
}
