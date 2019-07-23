﻿using System;
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

        public WebSocketState State => socket?.State ?? WebSocketState.Closed;

        public bool CloseRequest { get => closeRequest; set => closeRequest = value; }

        public event EventHandler<WebNotifyClientEventArgs> OnReceiveMessage;
        public event EventHandler<ExceptionEventArgs> OnError;
        public event EventHandler<EventArgs> OnClose;

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
                    if (ex.InnerException?.Message?.EndsWith("HRESULT: 0x80072EFE", StringComparison.OrdinalIgnoreCase) ?? false)
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
