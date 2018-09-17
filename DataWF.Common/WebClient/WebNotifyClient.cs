using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class WebNotifyClient
    {
        private ClientWebSocket socket;

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

        public async Task Listen()
        {
            while (socket.State != WebSocketState.Closed)
            {
                var recieve = await ReadString();
                OnReceiveMessage?.Invoke(this, new WebNotifyClientEventArgs(recieve));
            }
        }

        public async Task<string> ReadString()
        {
            var buffer = new byte[4 * 1024];
            var builder = new StringBuilder();
            WebSocketReceiveResult result = null;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.Count > 0)
                {
                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }

            } while (!result.EndOfMessage);
            return builder.ToString();
        }

    }
}
