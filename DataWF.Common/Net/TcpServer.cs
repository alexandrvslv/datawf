using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;

namespace DataWF.Common
{
    public class TcpServer : IDisposable
    {
        //http://stackoverflow.com/questions/5879605/udp-port-open-check-c-sharp
        public static int GetTcpPort()
        {
            var prop = IPGlobalProperties.GetIPGlobalProperties();
            var active = prop.GetActiveTcpListeners();

            int myport = 49152;
            for (; myport < 65535; myport++)
            {
                bool alreadyinuse = false;
                foreach (var p in active)
                {
                    if (p.Port == myport)
                    {
                        alreadyinuse = true;
                        break;
                    }
                }
                if (!alreadyinuse)
                {
                    var connections = prop.GetActiveTcpConnections();
                    foreach (var p in connections)
                        if (p.LocalEndPoint.Port == myport)
                        {
                            alreadyinuse = true;
                            break;
                        }

                    if (!alreadyinuse)
                        break;
                }
            }
            return myport;
        }

        public static int GetUdpPort()
        {
            var prop = IPGlobalProperties.GetIPGlobalProperties();
            var active = prop.GetActiveUdpListeners();
            int myport = 49152;
            for (; myport < 65535; myport++)
            {
                bool alreadyinuse = false;
                foreach (var p in active)
                    if (p.Port == myport)
                    {
                        alreadyinuse = true;
                        break;
                    }
                if (!alreadyinuse)
                {
                    break;
                }
            }
            return myport;
        }

        public static IPHostEntry GetHostEntry()
        {
            return Dns.GetHostEntry(Dns.GetHostName());
        }

        public static IPAddress GetAddress()
        {
            var entry = GetHostEntry();
            foreach (var address in entry.AddressList)
                if (address.AddressFamily == AddressFamily.InterNetwork)
                    return address;
            return null;
        }

        public static IPEndPoint ParseEndPoint(string address)
        {
            string[] split = address.Split(':');
            if (split.Length < 2)
                return null;
            IPAddress ipaddress = IPAddress.Parse(split[0]);
            int port = 0;
            if (int.TryParse(split[1], out port))
            {
                return new IPEndPoint(ipaddress, port);
            }
            return null;
        }


        static readonly int TimeoutTick = 5000;

        protected bool online = true;
        protected SelectableList<TcpSocket> clients = new SelectableList<TcpSocket>();
        protected TcpListener listener;
        protected IPEndPoint localPoint;
        protected ManualResetEvent acceptEvent = new ManualResetEvent(true);
        protected ManualResetEvent timeoutEvent = new ManualResetEvent(true);
        public event EventHandler<TcpServerEventArgs> DataLoad;
        public event EventHandler<TcpServerEventArgs> DataSend;
        public event EventHandler<TcpSocketEventArgs> ClientTimeout;
        public event EventHandler<TcpSocketEventArgs> ClientConnect;
        public event EventHandler<TcpSocketEventArgs> ClientDisconnect;
        public event EventHandler<TcpExceptionEventArgs> DataException;
        public event EventHandler Started;
        public event EventHandler Stopped;
        private bool logEvents;

        public TcpServer()
        {
            clients.Indexes.Add(new Invoker<TcpSocket, string>($"{nameof(TcpSocket.Point)}.{nameof(object.ToString)}",
                                                                     (item) => item.Point.ToString()));
            TimeOut = TimeSpan.MinValue;
        }

        public bool LogEvents
		{
			get { return logEvents; }
			set { logEvents = value; }
		}

        [Browsable(false)]
        public TimeSpan TimeOut { get; set; }

        [Browsable(false)]
        public IPEndPoint LocalPoint
        {
            get { return localPoint; }
            set { localPoint = value; }
        }

        [Browsable(false)]
        public TcpListener Listener
        {
            get { return listener; }
        }

        [Browsable(false)]
        public bool OnLine
        {
            get { return online && listener != null && listener.Server.IsBound; }
        }

        public void StartListener()
        {
            online = true;
            listener = new TcpListener(localPoint);
            listener.Start();

            ThreadPool.QueueUserWorkItem(p =>
                {
                    OnStart();
                    while (online)
                    {
                        acceptEvent.Reset();
                        listener.BeginAcceptSocket(AcceptCallback, new TcpSocketEventArgs());
                        acceptEvent.WaitOne();
                    }
                });

            if (TimeOut != TimeSpan.MinValue)
                ThreadPool.QueueUserWorkItem(p =>
                {
                    while (online)
                    {
                        timeoutEvent.Reset();
                        for (int i = 0; i < clients.Count; i++)
                        {
                            var client = clients[i];
                            if ((DateTime.Now - client.Stamp) > TimeOut)
                            {
                                OnClientTimeOut(client);
                                i--;
                            }
                        }
                        timeoutEvent.WaitOne(TimeoutTick);
                    }
                });
        }

        public void StopListener()
        {
            listener.Stop();
            online = false;
            acceptEvent.Set();
            foreach (var client in clients.ToArray())
                client.Dispose();
            clients.Clear();
            OnStop();
        }

        private void AcceptCallback(IAsyncResult result)
        {
            acceptEvent.Set();
            var arg = result.AsyncState as TcpSocketEventArgs;
            try
            {
                arg.Client = new TcpSocket { Server = this };
                arg.Client.Socket = listener.EndAcceptSocket(result);
                OnClientConnect(arg);
            }
            catch (Exception ex)
            {
                if (listener.Server.IsBound)
                    OnDataException(new TcpExceptionEventArgs(arg, ex));
            }
        }

        public TcpSocket NewClient(IPEndPoint address)
        {
            return NewClient(localPoint, address);
        }

        public TcpSocket NewClient(IPEndPoint local, IPEndPoint address)
        {
            var client = new TcpSocket { Server = this };
            client.Socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (local != null)
                client.Socket.Bind(local);
            client.Connect(address);
            return client;
        }

        public void Send(IPEndPoint address, string data)
        {
            Send(address, Encoding.UTF8.GetBytes(data));
        }

        public void Send(IPEndPoint address, byte[] data)
        {
            var client = clients.SelectOne("Point.ToString", CompareType.Equal, address.ToString());
            if (client == null)
                client = NewClient(localPoint, address);
            client.Send(data);
        }

        protected virtual void OnStart()
        {
            if (Started != null)
                Started(this, EventArgs.Empty);

            Helper.Logs.Add(new StateInfo("NetService", "Start", localPoint.ToString()));
        }

        protected virtual void OnStop()
        {
            if (Stopped != null)
                Stopped(this, EventArgs.Empty);

            Helper.Logs.Add(new StateInfo("NetService", "Stop", localPoint.ToString()));
        }

        protected internal void OnDataSend(TcpServerEventArgs arg)
        {
            NetStat.Set("Server Send", 1, arg.Length);

            if (logEvents)
                Helper.Logs.Add(new StateInfo("NetService", "DataSend", string.Format("{0} {1} {2}",
                    arg.Client.Socket.LocalEndPoint,
                    arg.Client.Socket.RemoteEndPoint,
                    Helper.TextDisplayFormat(arg.Length, "size"))));

            if (DataSend != null)
                DataSend(this, arg);

            if (arg.Stream is MemoryStream)
                arg.Stream.Dispose();
        }

        protected internal void OnDataLoad(TcpServerEventArgs arg)
        {
            NetStat.Set("Server Receive", 1, arg.Length);

            if (logEvents)
                Helper.Logs.Add(new StateInfo("NetService", "DataLoad", string.Format("{0} {1} {2}",
                    arg.Client.Socket.LocalEndPoint,
                    arg.Client.Socket.RemoteEndPoint,
                    Helper.TextDisplayFormat(arg.Length, "size"))));

            if (DataLoad != null)
                DataLoad(this, arg);

            arg.Stream.Dispose();
        }

        protected internal void OnDataException(TcpExceptionEventArgs arg)
        {
            if (DataException != null)
                DataException(this, arg);

            Helper.OnException(arg.Exception);

            if (arg.Arguments is TcpServerEventArgs && ((TcpServerEventArgs)arg.Arguments).Stream is MemoryStream)
                ((TcpServerEventArgs)arg.Arguments).Stream.Dispose();
        }

        protected internal void OnClientConnect(TcpSocketEventArgs arg)
        {
            clients.Add(arg.Client);
            arg.Client.Load();

            if (ClientConnect != null)
                ClientConnect(this, arg);

        }

        protected internal void OnClientDisconect(TcpSocketEventArgs arg)
        {
            if (ClientDisconnect != null)
                ClientDisconnect(this, arg);

            clients.Remove(arg.Client);
        }

        protected void OnClientTimeOut(TcpSocket client)
        {
            if (ClientTimeout != null)
                ClientTimeout(this, new TcpSocketEventArgs { Client = client });

            client.Dispose();
        }

        public void Remove(TcpSocket client)
        {
            client.Dispose();
        }

        public virtual void Dispose()
        {
            if (online)
                StopListener();
            acceptEvent?.Dispose();
            timeoutEvent?.Dispose();
        }

    }


}
