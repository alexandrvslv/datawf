using DataWF.Common;
using DataWF.WebService.Common;
using System;
using System.Net.WebSockets;
using System.Text.Json.Serialization;

[assembly: Invoker(typeof(WebNotifyConnection), nameof(WebNotifyConnection.Socket), typeof(WebNotifyConnection.SocketInvoker))]
[assembly: Invoker(typeof(WebNotifyConnection), nameof(WebNotifyConnection.User), typeof(WebNotifyConnection.UserInvoker))]
namespace DataWF.WebService.Common
{
    public class WebNotifyConnection : DefaultItem, IDisposable
    {
        private static uint IdSequence = 0;

        public WebNotifyConnection()
        {
            if (IdSequence == uint.MaxValue)
                IdSequence = 0;
            Id = ++IdSequence;
            Date = DateTime.Now;
        }

        public uint Id { get; set; }

        public DateTime Date { get; }

        public string UserEmail => User?.Name;

        public WebSocketState State => Socket?.State ?? WebSocketState.Aborted;

        public string Address { get; set; }

        public string Action { get; set; }

        [JsonIgnore]
        public WebSocket Socket { get; set; }

        [JsonIgnore]
        public IUserIdentity User { get; set; }

        public string Platform { get; set; }

        public string Application { get; set; }

        public string Version { get; set; }

        [JsonIgnore]
        public Version VersionValue { get; set; }

        public int ReceiveCount { get; set; }

        public long ReceiveLength { get; set; }

        public int SendCount { get; set; }

        public long SendLength { get; set; }

        public int SendErrors { get; set; }

        public string SendError { get; set; }

        public int SendingCount { get; internal set; }

        public void Dispose()
        {
            Socket?.Dispose();
            Socket = null;
        }

        public class SocketInvoker : Invoker<WebNotifyConnection, WebSocket>
        {
            public static readonly SocketInvoker Instance = new SocketInvoker();
            public override string Name => nameof(WebNotifyConnection.Socket);

            public override bool CanWrite => true;

            public override WebSocket GetValue(WebNotifyConnection target) => target.Socket;

            public override void SetValue(WebNotifyConnection target, WebSocket value) => target.Socket = value;
        }

        public class UserInvoker : Invoker<WebNotifyConnection, IUserIdentity>
        {
            public static readonly UserInvoker Instance = new UserInvoker();
            public override string Name => nameof(WebNotifyConnection.User);

            public override bool CanWrite => true;

            public override IUserIdentity GetValue(WebNotifyConnection target) => target.User;

            public override void SetValue(WebNotifyConnection target, IUserIdentity value) => target.User = value;
        }
    }


}
