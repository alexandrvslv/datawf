using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public abstract class SocketService : ISocketService
    {
        public static readonly int DefaultBufferSize = 8 * 1024;

        protected IListIndex<ISocketConnection, string> connectionNameIndex;
        protected readonly SelectableList<ISocketConnection> clients = new SelectableList<ISocketConnection>();
        protected bool online = true;

        public SocketService()
        {
            TransferTimeOut = TimeSpan.MinValue;
            connectionNameIndex = clients.Indexes.Add(new ActionInvoker<ISocketConnection, string>(SocketConnection.NameInvoker.Instance.Name,
                                                                                                    p => p.Name));
        }

        public virtual Uri Address { get; set; }

        IEnumerable<ISocketConnection> ISocketService.Clients => clients;
        public SelectableList<ISocketConnection> Clients => clients;

        public SocketCompressionMode Compression { get; set; } = SocketCompressionMode.Brotli;

        public virtual bool OnLine => online;

        public bool LogEvents { get; set; }

        public int BufferSize { get; set; }

        [Browsable(false)]
        public TimeSpan TransferTimeOut { get; set; }
        public TimeSpan ConnectionTimeOut { get; set; }

        public Func<SocketStreamArgs, Task> ParseDataLoad;
        public event EventHandler<SocketStreamArgs> DataLoad;
        public event EventHandler<SocketStreamArgs> DataSend;
        public event EventHandler<SocketConnectionArgs> ClientTimeout;
        public event EventHandler<SocketConnectionArgs> ClientConnect;
        public event EventHandler<SocketConnectionArgs> ClientDisconnect;
        public event EventHandler<SocketExceptionArgs> DataException;
        public event EventHandler Started;
        public event EventHandler Stopped;

        public abstract ValueTask<ISocketConnection> CreateClient(Uri address);
        public abstract ValueTask<ISocketConnection> CreateClient(object socket);
        protected abstract void BindSocket(int backlog);
        protected abstract void UnBindSocket();
        protected abstract Task MainLoop();

        public void StartListener(int backlog)
        {
            if (Address == null)
            {
                throw new Exception("LocalPoint not specified!");
            }
            BindSocket(backlog);
            online = true;

            if (TransferTimeOut != TimeSpan.MinValue)
                _ = Task.Run(TimeoutLoop);

            _ = MainLoop();

            OnStart();
        }

        public void StopListener()
        {
            foreach (var client in clients.ToList())
                try
                {
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            clients.Clear();

            UnBindSocket();

            online = false;

            OnStop();
        }


        public void WaitAll()
        {
            foreach (var client in clients)
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

        protected void LogEvent(SocketStreamArgs arg)
        {
            if (LogEvents)
            {
                Helper.Logs.Add(new StateInfo(nameof(TcpSocketService),
                    $"{arg.Mode} R:{arg.ReaderState} W:{arg.WriterState}",
                    $"Service: {Address} Connection: {arg.Connection.Address} Transferred: { Helper.SizeFormat(arg.Transfered)}"));
            }
        }

        public virtual object OnSended(SocketStreamArgs args)
        {
            NetStat.Set("Server Send", 1, args.Transfered);
            LogEvent(args);

            DataSend?.Invoke(this, args);
            return default;
        }

        public virtual async void OnReceiveStart(SocketStreamArgs args)
        {
            LogEvent(args);
            if (ParseDataLoad != null)
            {
                await ParseDataLoad(args);
            }
            else if (DataLoad != null)
            {
                await Task.Run(() => DataLoad(this, args));
            }
            else
            {
                await Task.Delay(1);
                throw new Exception("No data load listener specified!");
            }
            await args.CompleteRead();
        }

        public virtual object OnReceiveFinish(SocketStreamArgs args)
        {
            NetStat.Set("Server Receive", 1, args.Transfered);
            LogEvent(args);

            return Task.CompletedTask;
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
            if (clients.Remove(args.Connection))
            {
                ClientDisconnect?.Invoke(this, args);
                args.Connection.Dispose();
            }
            return default;
        }

        public virtual object OnClientConnect(SocketConnectionArgs arg)
        {
            _ = Task.Run(() => _ = arg.Connection.ListenerLoop());

            clients.Add(arg.Connection);
            ClientConnect?.Invoke(this, arg);

            return default;
        }

        protected virtual void OnStart()
        {
            Started?.Invoke(this, EventArgs.Empty);

            Helper.Logs.Add(new StateInfo(nameof(SocketService), "Start", Address.ToString()));
        }

        protected virtual void OnStop()
        {
            Stopped?.Invoke(this, EventArgs.Empty);

            Helper.Logs.Add(new StateInfo(nameof(SocketService), "Stop", Address.ToString()));
        }

        public virtual void Dispose()
        {
            if (online)
                StopListener();
            timeoutEvent?.Dispose();
        }
    }
    public class TcpSocketService : SocketService
    {
        protected readonly ManualResetEventSlim timeoutEvent = new ManualResetEventSlim(true);
        protected Socket socket;

        public TcpSocketService()
        {
        }

        public override Uri Address
        {
            get => base.Address;
            set
            {
                base.Address = value;
                Point = Address.ToEndPoint();
            }
        }

        [Browsable(false)]
        public IPEndPoint Point { get; set; }

        [Browsable(false)]
        public Socket Socket => socket;

        public override bool OnLine => online && (socket?.IsBound ?? false);

        private void TimeoutLoop()
        {
            while (online)
            {
                timeoutEvent.Reset();
                for (int i = 0; i < clients.Count; i++)
                {
                    var client = clients[i];
                    if ((DateTime.UtcNow - client.Stamp) > TransferTimeOut)
                    {
                        OnClientTimeOut(client);
                        i--;
                    }
                }
                timeoutEvent.Wait(TimeoutTick);
            }
        }

        protected override void BindSocket(int backlog)
        {
            socket = new Socket(Point.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(Point);
            socket.Listen(backlog);
        }

        protected override void UnBindSocket()
        {
            socket.Close();
            socket.Dispose();
            socket = null;
        }

        protected override async Task MainLoop()
        {
            while (OnLine)
            {
                var socket = new Socket(Point.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                try
                {

                    Debug.WriteLine($"TcpServer {Point} Start Accept");
                    socket = await Task.Factory.FromAsync<Socket>(Socket.BeginAccept(socket, 0, null, null), Socket.EndAccept);

                    Debug.WriteLine($"TcpServer {Point} Accept: {socket.RemoteEndPoint}");
                    var arg = new SocketConnectionArgs(new TcpSocketConnection
                    {
                        Server = this,
                        Socket = socket
                    });
                    _ = OnClientConnect(arg);
                }
                catch (Exception ex)
                {
                    socket?.Dispose();
                    if (OnLine)
                    {
                        OnDataException(new SocketExceptionArgs(SocketConnectionArgs.Default, ex));
                    }
                }
            }
        }

        public override async ValueTask<ISocketConnection> CreateClient(Uri address)
        {
            var client = new TcpSocketConnection { Server = this, Address = address };
            await client.Connect(Address);
            return client;
        }

        public override ValueTask<ISocketConnection> CreateClient(object socket)
        {
            if (!(socket is Socket tcpSocket))
            {
                throw new ArgumentException($"Expect {typeof(Socket)} type!", nameof(socket));
            }
            var client = new TcpSocketConnection { Server = this, Socket = tcpSocket };
            return client;
        }

        protected void OnClientTimeOut(ISocketConnection client)
        {
            if (client.Socket.Poll(5000, SelectMode.SelectRead) && (client.Socket.Available == 0))
            {
                ClientTimeout?.Invoke(this, new SocketEventArgs { TcpSocket = client });

                client.Dispose();
            }
            else
            {
                client.Stamp = DateTime.UtcNow;
            }
        }
    }
}
