﻿using DataWF.Common;
using DataWF.Data;
using Newtonsoft.Json;
using System;
using System.Net.WebSockets;

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

        public void Dispose()
        {
            Socket?.Dispose();
            Socket = null;
        }
    }

    [Invoker(typeof(WebNotifyConnection), nameof(WebNotifyConnection.Socket))]
    public class WebNotifyConnectionSocketInvoker : Invoker<WebNotifyConnection, WebSocket>
    {
        public static readonly WebNotifyConnectionSocketInvoker Instance = new WebNotifyConnectionSocketInvoker();
        public override string Name => nameof(WebNotifyConnection.Socket);

        public override bool CanWrite => true;

        public override WebSocket GetValue(WebNotifyConnection target) => target.Socket;

        public override void SetValue(WebNotifyConnection target, WebSocket value) => target.Socket = value;
    }

    [Invoker(typeof(WebNotifyConnection), nameof(WebNotifyConnection.User))]
    public class WebNotifyConnectionUserInvoker : Invoker<WebNotifyConnection, IUserIdentity>
    {
        public static readonly WebNotifyConnectionUserInvoker Instance = new WebNotifyConnectionUserInvoker();
        public override string Name => nameof(WebNotifyConnection.User);

        public override bool CanWrite => true;

        public override IUserIdentity GetValue(WebNotifyConnection target) => target.User;

        public override void SetValue(WebNotifyConnection target, IUserIdentity value) => target.User = value;
    }
}