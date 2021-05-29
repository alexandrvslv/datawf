using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public partial class WebSocketConnection : SocketConnection
    {
        private static readonly ArraySegment<byte> finSegment = new ArraySegment<byte>(fin);
        public WebSocketConnection()
        {

        }

        public WebSocketState State => Socket?.State ?? WebSocketState.Aborted;

        public string Action { get; set; }

        [JsonIgnore]
        public WebSocket Socket { get; set; }

        public override bool Connected => Socket != null
                 && (State == WebSocketState.Open
                 || State == WebSocketState.Connecting);

        protected override async Task<int> SendPart(SocketStreamArgs arg, int read)
        {
            await Socket.SendAsync(arg.Buffer.Slice(0, read),
                                    WebSocketMessageType.Binary,
                                    false,
                                    arg.CancellationToken?.Token ?? CancellationToken.None);

            return read;
        }

        protected override async Task SendFin(SocketStreamArgs arg)
        {
            await Socket.SendAsync(finSegment,
                                    WebSocketMessageType.Binary,
                                    true,
                                    arg.CancellationToken?.Token ?? CancellationToken.None);
        }

        protected override async Task<int> LoadPart(SocketStreamArgs arg)
        {
            var memory = arg.Pipe.Writer.GetMemory(arg.Buffer.Count + finLength);
            var result = await Socket.ReceiveAsync(memory, arg.CancellationToken?.Token ?? CancellationToken.None);
            switch (result.MessageType)
            {
                case WebSocketMessageType.Binary:
                case WebSocketMessageType.Text:
                    return result.Count;
                case WebSocketMessageType.Close:
                    await Disconnect();
                    throw new Exception();
            }
            return result.Count;
        }

        public override void Dispose()
        {
            if (!disposed)
            {
                base.Dispose();
                Socket?.Dispose();
                Socket = null;
            }
        }

        public override async ValueTask Connect(Uri address)
        {
            var arg = new SocketEventArgs { Socket = this };

            try
            {
                var client = new WebSocketClient();
                await client.ConnectAsync(address, CancellationToken.None);
                Socket = client;
                Stamp = DateTime.UtcNow;
                _ = Server.OnClientConnect(arg);                
            }
            catch (Exception ex)
            {
                Server.OnDataException(new SocketExceptionEventArgs(arg, ex));
                throw;
            }
        }

        public override async ValueTask Disconnect()
        {
            var arg = new SocketEventArgs { Socket = this };
            try
            {
                await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good luck!", CancellationToken.None);
                _ = Server.OnClientDisconect(arg);
            }
            catch (Exception ex)
            {
                Server.OnDataException(new SocketExceptionEventArgs(arg, ex));
            }
        }
    }
}
