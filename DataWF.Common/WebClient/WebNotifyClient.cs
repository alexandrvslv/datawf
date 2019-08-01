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
        private bool closeRequest;
        private Uri uri;

        public WebSocketState State => socket?.State ?? WebSocketState.Closed;

        public bool CloseRequest { get => closeRequest; set => closeRequest = value; }

        public event EventHandler<WebNotifyClientEventArgs> OnReceiveMessage;
        public event EventHandler<ExceptionEventArgs> OnError;
        public event EventHandler<EventArgs> OnClose;
        public event EventHandler<EventArgs> OnOpen;

        public Task RegisterNotify(Uri uri, string autorization)
        {
            socket = new ClientWebSocket();
            if (!string.IsNullOrEmpty(autorization))
            {
                socket.Options.SetRequestHeader("Authorization", autorization);
            }
            this.uri = uri;
            return Task.CompletedTask;
        }

        public async void Listen()
        {
            await socket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
            OnOpen?.Invoke(this, EventArgs.Empty);
            await Run();
        }

        public async Task Run()
        {
            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    var recieve = await ReadData();
                    if (recieve != null)
                    {
                        OnReceiveMessage?.Invoke(this, new WebNotifyClientEventArgs(recieve));
                    }
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    if (socket.State != WebSocketState.Open)
                    {
                        OnClose(this, EventArgs.Empty);
                        return;
                    }
                    OnError?.Invoke(this, new ExceptionEventArgs(ex));
                }
            }
        }

        public async Task<byte[]> ReadData()
        {
            var buffer = new ArraySegment<byte>(new byte[8 * 1024]);
            using (var builder = new MemoryStream())
            {
                WebSocketReceiveResult result = null;
                do
                {
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        OnClose?.Invoke(this, EventArgs.Empty);
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
            try
            {
                closeRequest = true;
                if (socket != null && socket.State == WebSocketState.Open)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Goodby", CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
            }
            finally
            {
                closeRequest = false;
                socket?.Dispose();
            }
        }

        public void Dispose()
        {
            Close().Wait();
            socket?.Dispose();
            socket = null;
        }

        public async void Send(byte[] data)
        {
            var buffer = new ArraySegment<byte>(new byte[8 * 1024]);
            using (var stream = new MemoryStream(data))
            {
                int result = 0;
                int total = 0;
                while ((result = stream.Read(buffer.Array, 0, buffer.Count)) != 0)
                {
                    total += result;
                    await socket.SendAsync(buffer, WebSocketMessageType.Binary, total >= data.Length, CancellationToken.None);
                }
            }

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
