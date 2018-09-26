using DataWF.Common;
using DataWF.Module.Common;
using System;
using System.Net.WebSockets;

namespace DataWF.Web.Common
{
    public class WebNotifyClient : IDisposable
    {
        public static readonly Invoker<WebNotifyClient, WebSocket> SocketInvoker = new Invoker<WebNotifyClient, WebSocket>(nameof(Socket), (p) => p.Socket, (p, v) => p.Socket = v);
        public static readonly Invoker<WebNotifyClient, User> UserInvoker = new Invoker<WebNotifyClient, User>(nameof(Socket), (p) => p.User, (p, v) => p.User = v);

        public WebSocket Socket { get; set; }
        public User User { get; set; }

        public void Dispose()
        {
            Socket?.Dispose();
            Socket = null;
        }
    }

}
