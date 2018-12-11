using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class WebNotifyClient : IDisposable, IWebNotifyClient
    {
        private ClientWebSocket socket;

        public WebSocketState State => socket?.State ?? WebSocketState.Closed;

        public event EventHandler<WebNotifyClientEventArgs> OnReceiveMessage;

        public async Task RegisterNotify(Uri uri, string autorization)
        {
            socket = new ClientWebSocket();
            if (!string.IsNullOrEmpty(autorization))
            {
                socket.Options.SetRequestHeader("Authorization", autorization);
            }

            await socket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
        }

        public void Listen()
        {
            _ = Run();
        }

        public async Task Run()
        {
            while (socket.State != WebSocketState.Closed)
            {
                var recieve = await ReadData();
                if (recieve != null)
                {
                    OnReceiveMessage?.Invoke(this, new WebNotifyClientEventArgs(recieve));
                }
            }
        }

        public async Task<byte[]> ReadData()
        {
            //var buffer = new byte[4 * 1024];
            var buffer = new ArraySegment<byte>(new byte[8192]);
            using (var builder = new MemoryStream())
            {
                WebSocketReceiveResult result = null;
                do
                {
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return null;
                    }
                    if (result.Count > 0)
                    {
                        builder.Write(buffer.Array, buffer.Offset, result.Count);
                    }

                } while (!result.EndOfMessage);
                return builder.ToArray();
            }
        }

        public async Task Close()
        {
            if (socket != null)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Goodby", CancellationToken.None).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            if (State == WebSocketState.Open)
            {
                Close().Wait();
            }

            socket?.Dispose();
            socket = null;
        }
    }

    public class WebNotifyItem
    {
        public string Type { get; set; }
        public List<WebNotifyEntry> Items { get; set; }
    }

    public class WebNotifyEntry
    {
        public string Id { get; set; }
        public int Diff { get; set; }
        public int User { get; set; }
    }
}
