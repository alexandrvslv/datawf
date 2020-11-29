using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{

    public class TcpServer : IDisposable
    {
        static readonly int TimeoutTick = 5000;

        protected bool online = true;
        protected readonly SelectableList<TcpSocket> clients = new SelectableList<TcpSocket>();
        protected readonly ManualResetEventSlim timeoutEvent = new ManualResetEventSlim(true);
        protected Socket socket;
        protected IPEndPoint point;
        private bool logEvents;

        public TcpServer()
        {
            clients.Indexes.Add(new ActionInvoker<TcpSocket, string>(nameof(TcpSocket.PointName),
                                                                     (item) => item.PointName));
            TimeOut = TimeSpan.MinValue;
        }

        public bool LogEvents
        {
            get => logEvents;
            set => logEvents = value;
        }

        public SelectableList<TcpSocket> Clients => clients;

        [Browsable(false)]
        public TimeSpan TimeOut { get; set; }

        [Browsable(false)]
        public IPEndPoint Point
        {
            get => point;
            set => point = value;
        }

        [Browsable(false)]
        public Socket Socket => socket;

        [Browsable(false)]
        public bool OnLine => online && (socket?.IsBound ?? false);

        public TcpServerCompressionMode Compression { get; set; } = TcpServerCompressionMode.Brotli;

        public Func<TcpStreamEventArgs, Task> ParseDataLoad;
        public event EventHandler<TcpStreamEventArgs> DataLoad;
        public event EventHandler<TcpStreamEventArgs> DataSend;
        public event EventHandler<TcpSocketEventArgs> ClientTimeout;
        public event EventHandler<TcpSocketEventArgs> ClientConnect;
        public event EventHandler<TcpSocketEventArgs> ClientDisconnect;
        public event EventHandler<TcpExceptionEventArgs> DataException;
        public event EventHandler Started;
        public event EventHandler Stopped;

        public void StartListener(int backlog)
        {
            if (Point == null)
            {
                throw new Exception("LocalPoint not specified!");
            }
            socket = new Socket(Point.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(Point);
            socket.Listen(backlog);
            online = true;

            if (TimeOut != TimeSpan.MinValue)
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

            socket.Close();
            socket.Dispose();
            socket = null;

            online = false;

            OnStop();
        }

        private void TimeoutLoop()
        {
            while (online)
            {
                timeoutEvent.Reset();
                for (int i = 0; i < clients.Count; i++)
                {
                    var client = clients[i];
                    if ((DateTime.UtcNow - client.Stamp) > TimeOut)
                    {
                        OnClientTimeOut(client);
                        i--;
                    }
                }
                timeoutEvent.Wait(TimeoutTick);
            }
        }

        private async Task MainLoop()
        {
            while (OnLine)
            {
                var socket = new Socket(Point.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                try
                {
                    var arg = new TcpSocketEventArgs();
                    Debug.WriteLine($"TcpServer {Point} Start Accept");
                    socket = await Task.Factory.FromAsync<Socket>(Socket.BeginAccept(socket, 0, null, arg), Socket.EndAccept);

                    Debug.WriteLine($"TcpServer {Point} Accept: {socket.RemoteEndPoint}");
                    arg.TcpSocket = new TcpSocket
                    {
                        Server = this,
                        Socket = socket
                    };
                    _ = OnClientConnect(arg);
                }
                catch (Exception ex)
                {
                    socket?.Dispose();
                    if (OnLine)
                    {
                        OnDataException(new TcpExceptionEventArgs(TcpSocketEventArgs.Empty, ex));
                    }
                }
            }
        }

        public ValueTask<TcpSocket> CreateClient(IPEndPoint address)
        {
            return CreateClient(point, address);
        }

        public async ValueTask<TcpSocket> CreateClient(IPEndPoint local, IPEndPoint address)
        {
            var client = new TcpSocket { Server = this, Point = address };
            await client.Connect(local);
            return client;
        }

        public Task<bool> Send(IPEndPoint address, string data)
        {
            return Send(address, Encoding.UTF8.GetBytes(data));
        }

        public async Task<bool> Send(IPEndPoint address, byte[] data)
        {
            var client = clients.SelectOne(nameof(TcpSocket.PointName), CompareType.Equal, address.ToString());
            if (client == null)
            {
                client = await CreateClient(point, address);
            }

            return await client.Send(data);
        }

        public async Task<bool> SendElement<T>(IPEndPoint address, T element)
        {
            var client = clients.SelectOne(nameof(TcpSocket.PointName), CompareType.Equal, address.ToString());
            if (client == null)
            {
                client = await CreateClient(point, address);
            }

            return await client.SendElement(element);
        }

        protected virtual void OnStart()
        {
            Started?.Invoke(this, EventArgs.Empty);

            Helper.Logs.Add(new StateInfo(nameof(TcpServer), "Start", point.ToString()));
        }

        protected virtual void OnStop()
        {
            Stopped?.Invoke(this, EventArgs.Empty);

            Helper.Logs.Add(new StateInfo(nameof(TcpServer), "Stop", point.ToString()));
        }

        protected virtual internal ValueTask OnDataSend(TcpStreamEventArgs arg)
        {
            NetStat.Set("Server Send", 1, arg.Transfered);

            if (logEvents)
            {
                Helper.Logs.Add(new StateInfo(nameof(TcpServer), "DataSend", string.Format("{0} {1} {2}",
                    arg.TcpSocket.Socket.LocalEndPoint,
                    arg.TcpSocket.Socket.RemoteEndPoint,
                    Helper.TextDisplayFormat(arg.Transfered, "size"))));
            }

            DataSend?.Invoke(this, arg);
            return default;
        }

        protected virtual internal async Task OnDataLoadStart(TcpStreamEventArgs arg)
        {
            if (logEvents)
            {
                Helper.Logs.Add(new StateInfo(nameof(TcpServer), "DataLoad", string.Format("{0} {1} {2}",
                    arg.TcpSocket.Socket.LocalEndPoint,
                    arg.TcpSocket.Socket.RemoteEndPoint,
                    Helper.TextDisplayFormat(arg.Transfered, "size"))));
            }
            if (ParseDataLoad != null)
            {
                await ParseDataLoad(arg);
            }
            else if (DataLoad != null)
            {
                await Task.Run(() => DataLoad(this, arg));
            }
            else
            {
                await Task.Delay(1);
                throw new Exception("No data load listener specified!");
            }
            arg.CompleteRead();
            await arg.ReleasePipe();
        }

        protected virtual internal Task OnDataLoadEnd(TcpStreamEventArgs arg)
        {
            NetStat.Set("Server Receive", 1, arg.Transfered);

            if (logEvents)
            {
                Helper.Logs.Add(new StateInfo(nameof(TcpServer), "DataLoad", string.Format("{0} {1} {2}",
                    arg.TcpSocket.Socket.LocalEndPoint,
                    arg.TcpSocket.Socket.RemoteEndPoint,
                    Helper.TextDisplayFormat(arg.Transfered, "size"))));
            }
            return Task.CompletedTask;
        }

        protected virtual internal void OnDataException(TcpExceptionEventArgs arg)
        {
            DataException?.Invoke(this, arg);

            Helper.OnException(arg.Exception);

            if (arg.Arguments is TcpStreamEventArgs tcpArgs && tcpArgs.ReaderStream is MemoryStream)
            {
                tcpArgs.ReaderStream.Dispose();
            }
        }

        public void WaitAll()
        {
            foreach (var client in clients)
            {
                client.WaitAll();
            }
        }

        protected internal ValueTask OnClientConnect(TcpSocketEventArgs arg)
        {
            _ = Task.Run(() => _ = arg.TcpSocket.ListenerLoop());

            clients.Add(arg.TcpSocket);
            ClientConnect?.Invoke(this, arg);

            return default;
        }

        protected internal ValueTask OnClientDisconect(TcpSocketEventArgs arg)
        {
            if (clients.Remove(arg.TcpSocket))
            {
                ClientDisconnect?.Invoke(this, arg);
                arg.TcpSocket.Dispose();
            }
            return default;
        }

        protected void OnClientTimeOut(TcpSocket client)
        {
            if (client.Socket.Poll(5000, SelectMode.SelectRead) && (client.Socket.Available == 0))
            {
                ClientTimeout?.Invoke(this, new TcpSocketEventArgs { TcpSocket = client });

                client.Dispose();
            }
            else
            {
                client.Stamp = DateTime.UtcNow;
            }
        }

        public void Remove(TcpSocket client)
        {
            client.Dispose();
        }

        public virtual void Dispose()
        {
            if (online)
                StopListener();
            timeoutEvent?.Dispose();
        }

    }


}
