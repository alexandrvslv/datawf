using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace DataWF.WebService.Common
{
    public class WebNotifyService : IWebNotifyService
    {
        protected readonly SelectableList<WebNotifyConnection> connections = new SelectableList<WebNotifyConnection>();

        public static WebNotifyService Instance { get; private set; }

        private readonly ConcurrentQueue<NotifyDBItem> buffer = new ConcurrentQueue<NotifyDBItem>();
        private readonly ManualResetEventSlim runEvent = new ManualResetEventSlim(false);
        private const int timer = 2000;

        public WebNotifyService()
        {
            Instance = this;
            connections.Indexes.Add(WebNotifyConnection.UserInvoker.Instance.Name,
                new ListIndex<WebNotifyConnection, IUserIdentity>(
                    WebNotifyConnection.UserInvoker.Instance,
                    NullUser.Value));
        }

        public event EventHandler<WebNotifyEventArgs> ReceiveMessage;
        public event EventHandler<WebNotifyEventArgs> RemoveConnection;

        public void Start()
        {
            DBService.AddRowAccept(OnAccept);
            runEvent.Reset();
            _ = SendChangesRunner();
        }

        public void Stop()
        {
            DBService.RemoveRowAccept(OnAccept);
            runEvent.Set();
        }

        protected ValueTask OnAccept(DBItemEventArgs arg)
        {
            var item = arg.Item;

            if (!(item is DBLogItem) && item.Table.Type == DBTableType.Table && item.Table.IsLoging)
            {
                var type = (arg.State & DBUpdateState.Delete) == DBUpdateState.Delete ? DBLogType.Delete
                    : (arg.State & DBUpdateState.Insert) == DBUpdateState.Insert ? DBLogType.Insert
                    : DBLogType.Update;
                buffer.Enqueue(new NotifyDBItem()
                {
                    Value = item,
                    Id = item.PrimaryId,
                    Command = type,
                    UserId = arg.User?.Id ?? 0
                });
            }
            return default;
        }

        private async Task SendChangesRunner()
        {
            while (!runEvent.Wait(timer))
            {
                try
                {
                    if (buffer.Count == 0)
                        continue;
                    var list = NotifyService.Dequeu(buffer);

                    await SendToAll(list);
                }
                catch (Exception e)
                {
                    Helper.OnException(e);
                }
            }
        }

        public WebNotifyConnection GetBySocket(WebSocket socket)
        {
            return connections.SelectOne(nameof(WebNotifyConnection.Socket), socket);
        }

        public IEnumerable<WebNotifyConnection> GetByUser(IUserIdentity user)
        {
            return connections.Select(WebNotifyConnection.UserInvoker.Instance, CompareType.Equal, user);
        }

        public void SetCurrentAction(ActionExecutingContext context)
        {
            var user = context.HttpContext.User?.GetCommonUser();
            SetCurrentAction(user, context);
        }

        public void SetCurrentAction(IUserIdentity user, ActionExecutingContext context)
        {
            SetCurrentAction(user, context.HttpContext.Connection.RemoteIpAddress.ToString(), context.ActionDescriptor.DisplayName);
        }

        public void SetCurrentAction(IUserIdentity user, string address, string action)
        {
            if (user != null)
            {
                foreach (var connection in GetByUser(user).Where(p => p.Address.Equals(address, StringComparison.Ordinal)))
                {
                    connection.Action = action;
                }
            }
            Helper.Logs.Add(new StateInfo("Web Request", action, address) { User = user?.Name });
        }

        public virtual WebNotifyConnection Register(WebSocket socket, IUserIdentity user, string address)
        {
            var connection = GetBySocket(socket);
            if (connection == null)
            {
                connection = new WebNotifyConnection
                {
                    Socket = socket,
                    User = user,
                    Address = address,
                };
                connections.Add(connection);
            }
            return connection;
        }

        public IEnumerable<WebNotifyConnection> GetConnections()
        {
            return connections;
        }

        public async void CloseAsync(DBUser user)
        {
            foreach (var item in GetByUser(user))
            {
                await CloseAsync(item);
            }
        }

        public async void CloseAsync(WebSocket socket)
        {
            await CloseAsync(GetBySocket(socket));
        }

        public async Task CloseAsync(WebNotifyConnection connection)
        {
            if (connection != null)
            {
                await connection.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Internal Server Close.", CancellationToken.None);
                Remove(connection);
            }
        }

        public virtual bool Remove(WebNotifyConnection connection)
        {
            var removed = false;
            try
            {
                if ((removed = connections.Remove(connection)))
                {
                    Debug.WriteLine($"Remove webSocket from {connection?.UserEmail}");
                    connection?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
            }

            RemoveConnection?.Invoke(this, new WebNotifyEventArgs(connection));
            return removed;
        }

        //https://github.com/radu-matei/websocket-manager/blob/blog-article/src/WebSocketManager/WebSocketManagerMiddleware.cs
        public async Task ListenAsync(WebNotifyConnection connection)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            while (connection.CheckConnection())
            {
                try
                {
                    WebSocketReceiveResult result = null;
                    var stream = new MemoryStream();
                    do
                    {
                        result = await connection.Socket.ReceiveAsync(buffer, CancellationToken.None);
                        if (result.Count > 0)
                        {
                            stream.Write(buffer.Array, buffer.Offset, result.Count);
                        }
                    }
                    while (!result.EndOfMessage);

                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Binary:
                        case WebSocketMessageType.Text:
                            _ = OnMessageReceive(connection, stream);
                            break;
                        case WebSocketMessageType.Close:
                            stream.Dispose();
                            await connection.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good luck!", CancellationToken.None);
                            Remove(connection);
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            }
            Remove(connection);
        }

        protected virtual async Task<object> OnMessageReceive(WebNotifyConnection connection, MemoryStream stream)
        {
            await Task.Delay(10).ConfigureAwait(false);

            object obj = connection.LoadMessage(stream);
            if (obj is WebNotifyRegistration registration)
            {
                connection.Platform = registration.Platform;
                connection.Application = registration.Application;
                connection.Version = registration.Version;
                connection.VersionValue = Version.TryParse(connection.Version, out var version) ? version : new Version("1.0.0.0");
            }
            ReceiveMessage?.Invoke(this, new WebNotifyEventArgs(connection, obj));
            return obj;
        }

        private async Task SendToAll(List<NotifyDBTable> list)
        {
            //var tasks = new List<Task>();
            foreach (var connection in connections.ToList())
            {
                try
                {
                    if (!(connection?.CheckConnection() ?? false))
                    {
                        Remove(connection);
                        continue;
                    }
                    //tasks.Add(connection.SendData(list));
                    await connection.SendData(list);
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            }
            //await Task.WhenAll(tasks);
        }
    }
}
