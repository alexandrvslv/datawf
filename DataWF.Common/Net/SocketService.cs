using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public abstract class SocketService : ISocketService
    {
        public static readonly int DefaultBufferSize = 16 * 1024;
        public static readonly int DefaultTransferTimeout = 10000;

        protected IListIndex<ISocketConnection, string> connectionNameIndex;
        protected readonly SelectableList<ISocketConnection> connections = new SelectableList<ISocketConnection>();
        protected readonly ManualResetEventSlim timeoutEvent = new ManualResetEventSlim(true);
        protected bool online = true;

        public SocketService()
        {
            BufferSize = DefaultBufferSize;
            TransferTimeout = TimeSpan.FromMilliseconds(DefaultTransferTimeout);
            connectionNameIndex = connections.Indexes.Add(new ActionInvoker<ISocketConnection, string>(SocketConnection.NameInvoker.Instance.Name,
                                                                                                    p => p.Name));
        }

        public virtual Uri Address { get; set; }

        IEnumerable<ISocketConnection> ISocketService.Connections => connections;
        public SelectableList<ISocketConnection> Connections => connections;

        public SocketCompressionMode Compression { get; set; } = SocketCompressionMode.Brotli;

        public virtual bool OnLine => online;

        public bool LogEvents { get; set; }

        public int BufferSize { get; set; }

        [Browsable(false)]
        public TimeSpan TransferTimeout { get; set; }
        public TimeSpan ConnectionTimeOut { get; set; }

        public event EventHandler<SocketStreamArgs> DataLoad;
        public event EventHandler<SocketStreamArgs> DataSend;
        public event EventHandler<SocketConnectionArgs> ClientTimeout;
        public event EventHandler<SocketConnectionArgs> ClientConnect;
        public event EventHandler<SocketConnectionArgs> ClientDisconnect;
        public event EventHandler<SocketExceptionArgs> DataException;
        public event EventHandler Started;
        public event EventHandler Stopped;

        public abstract ValueTask<ISocketConnection> CreateConnection(Uri address);
        public abstract ValueTask<ISocketConnection> CreateConnection(object socket);
        protected abstract void BindSocket(int backlog);
        protected abstract void UnBindSocket();
        protected abstract Task MainLoop();

        public void StartListener(int backlog)
        {
            if (Address == null)
            {
                throw new Exception("Address not specified!");
            }
            if (OnLine)
            {
                throw new Exception("Is on line!");
            }
            BindSocket(backlog);
            online = true;

            if (ConnectionTimeOut != default(TimeSpan))
                _ = Task.Run(TimeoutLoop);

            _ = MainLoop();

            OnStart();
        }

        public void StopListener()
        {
            foreach (var client in connections.ToList())
                try
                {
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            connections.Clear();

            UnBindSocket();

            online = false;

            OnStop();
        }

        protected void TimeoutLoop()
        {
            while (online)
            {
                timeoutEvent.Reset();
                for (int i = 0; i < connections.Count; i++)
                {
                    var client = connections[i];
                    if ((DateTime.UtcNow - client.Stamp) > TransferTimeout)
                    {
                        client.OnTimeOut();
                        i--;
                    }
                }
                timeoutEvent.Wait(ConnectionTimeOut);
            }
        }

        public void WaitAll()
        {
            foreach (var client in connections)
            {
                client.WaitAll();
            }
        }

        public ISocketConnection GetConnection(Uri address)
        {
            return GetConnection(address.ToString());
        }

        public ISocketConnection GetConnection(string name)
        {
            return connectionNameIndex.SelectOne(name);
        }

        public void Remove(ISocketConnection client)
        {
            client.Dispose();
        }

        public void LogEvent(SocketStreamArgs arg)
        {
            if (LogEvents)
            {
                var message = $"{arg.Mode} R:{arg.ReaderState} W:{arg.WriterState}";
                var description = $"Service: {Address} Connection: {arg.Connection.Address} Transferred: { Helper.SizeFormat(arg.Transfered)} ({arg.PartsCount})";
                this.Log(description, message: message);
            }
        }

        public virtual object OnSended(SocketStreamArgs args)
        {
            NetStat.Set("Server Send", 1, args.Transfered);
            LogEvent(args);

            DataSend?.Invoke(this, args);
            return default;
        }

        public virtual ValueTask OnReceiveStart(SocketStreamArgs args)
        {
            return default;
        }

        public virtual ValueTask OnReceiveFinish(SocketStreamArgs args)
        {
            NetStat.Set("Server Receive", 1, args.Transfered);
            LogEvent(args);

            return default;
        }

        public virtual void OnDataException(SocketExceptionArgs args)
        {
            DataException?.Invoke(this, args);

            Helper.OnException(args.Exception);

            if (args.Arguments is SocketStreamArgs tcpArgs && tcpArgs.ReaderStream is MemoryStream)
            {
                tcpArgs.ReaderStream.Dispose();
            }
        }

        public virtual object OnClientDisconect(SocketConnectionArgs args)
        {
            if (connections.Remove(args.Connection))
            {
                ClientDisconnect?.Invoke(this, args);
                args.Connection.Dispose();
            }
            return default;
        }

        public virtual object OnClientConnect(SocketConnectionArgs arg)
        {
            _ = Task.Run(() => _ = arg.Connection.ListenerLoop());

            connections.Add(arg.Connection);
            ClientConnect?.Invoke(this, arg);

            return default;
        }

        public virtual object OnClientTimeout(SocketConnectionArgs arg)
        {
            ClientTimeout?.Invoke(this, arg);
            return default;
        }

        protected virtual void OnStart()
        {
            Started?.Invoke(this, EventArgs.Empty);

            this.Log(Address.ToString());
        }

        protected virtual void OnStop()
        {
            Stopped?.Invoke(this, EventArgs.Empty);

            this.Log(Address.ToString());
        }

        public virtual void Dispose()
        {
            if (online)
                StopListener();
            timeoutEvent?.Dispose();
        }
    }
}
