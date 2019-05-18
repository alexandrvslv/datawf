using DataWF.Common;
using DataWF.Module.Common;
using Newtonsoft.Json;
using System;
using System.Net.WebSockets;

namespace DataWF.Web.Common
{
    public class WebNotifyConnection : DefaultItem, IDisposable
    {
        private static uint IdSequence = 0;
        public static readonly Invoker<WebNotifyConnection, WebSocket> SocketInvoker = new Invoker<WebNotifyConnection, WebSocket>(nameof(Socket), (p) => p.Socket, (p, v) => p.Socket = v);
        public static readonly Invoker<WebNotifyConnection, User> UserInvoker = new Invoker<WebNotifyConnection, User>(nameof(User), (p) => p.User, (p, v) => p.User = v);

        public WebNotifyConnection()
        {
            if (IdSequence == uint.MaxValue)
                IdSequence = 0;
            Id = ++IdSequence;
            Date = DateTime.Now;
        }

        public uint Id { get; set; }

        public DateTime Date { get; }

        public string UserEmail => User?.EMail;

        public WebSocketState State => Socket?.State ?? WebSocketState.Aborted;

        public string Address { get; set; }

        public string Action { get; set; }

        [JsonIgnore]
        public WebSocket Socket { get; set; }

        [JsonIgnore]
        public User User { get; set; }

        public void Dispose()
        {
            Socket?.Dispose();
            Socket = null;
        }
    }

}
