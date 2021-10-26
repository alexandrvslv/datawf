using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DataWF.Common
{
    public class TcpSocket : IDisposable
    {
        protected ManualResetEvent sendEvent = new ManualResetEvent(true);
        protected ManualResetEvent loadEvent = new ManualResetEvent(true);
        protected ManualResetEvent connectEvent = new ManualResetEvent(true);
        protected ManualResetEvent disconnectEvent = new ManualResetEvent(true);
        private System.Net.Sockets.Socket socket;

        public TcpSocket()
        {
            Stamp = DateTime.Now;
        }

        public DateTime Stamp { get; set; }

        public TcpServer Server { get; set; }

        public Socket Socket
        {
            get { return socket; }
            set
            {
                socket = value;
                Point = (IPEndPoint)socket.RemoteEndPoint;
            }
        }

        public IPEndPoint Point { get; set; }

        public IPEndPoint LocalPoint { get { return socket == null ? null : (IPEndPoint)socket.LocalEndPoint; } }

        public void Send(byte[] data)
        {
            Send(new MemoryStream(data));
        }

        public void Send(Stream stream)
        {
            Send(new TcpServerEventArgs { Stream = stream });
        }

        public void Send(TcpServerEventArgs arg)
        {
            arg.Client = this;
            arg.Stream.Position = 0;
            if (arg.Stream.Length > 0)
            {
                sendEvent.Reset();
                var read = arg.Stream.Read(arg.Buffer, 0, TcpServerEventArgs.BufferSize);
                Socket.BeginSend(arg.Buffer, 0, read, SocketFlags.None, SendCallback, arg);
                sendEvent.WaitOne();
            }
        }

        private void SendCallback(IAsyncResult result)
        {
            var arg = result.AsyncState as TcpServerEventArgs;
            try
            {
                var sended = Socket.EndSend(result);
                if (sended > 0)
                {
                    var read = arg.Stream.Read(arg.Buffer, 0, TcpServerEventArgs.BufferSize);
                    if (read > 0)
                    {
                        Socket.BeginSend(arg.Buffer, 0, read, SocketFlags.None, SendCallback, arg);
                        return;
                    }
                }
                sendEvent.Set();
                Stamp = DateTime.Now;
                Server.OnDataSend(arg);
            }
            catch (Exception ex)
            {
                sendEvent.Set();
                Server.OnDataException(new TcpExceptionEventArgs(arg, ex));
            }
        }

        internal void Load()
        {
            ThreadPool.QueueUserWorkItem(p =>
                {
                    while (Socket.Connected)
                    {
                        var arg = new TcpServerEventArgs
                        {
                            Client = this,
                            Stream = new MemoryStream()
                        };
                        loadEvent.Reset();
                        Socket.BeginReceive(arg.Buffer, 0, TcpServerEventArgs.BufferSize, SocketFlags.None, LoadCallback, arg);
                        loadEvent.WaitOne();
                    }
                });
        }

        private void LoadCallback(IAsyncResult result)
        {
            var arg = result.AsyncState as TcpServerEventArgs;
            try
            {
                int read = Socket.EndReceive(result);
                if (read > 0)
                {
                    arg.Stream.Write(arg.Buffer, 0, read);
                    if (read == TcpServerEventArgs.BufferSize)
                    {
                        Socket.BeginReceive(arg.Buffer, 0, TcpServerEventArgs.BufferSize, SocketFlags.None, LoadCallback, arg);
                        return;
                    }
                    loadEvent.Set();
                    Stamp = DateTime.Now;
                    arg.Stream.Position = 0;
                    Server.OnDataLoad(arg);
                }
                else
                    Disconnect(true);
            }
            catch (Exception ex)
            {
                loadEvent.Set();
                Server.OnDataException(new TcpExceptionEventArgs(arg, ex));
            }
        }

        public void Connect(IPEndPoint address)
        {
            connectEvent.WaitOne();
            connectEvent.Reset();
            if (!Socket.Connected)
            {
                Socket.BeginConnect(address, ConnectCallback, new TcpSocketEventArgs { Client = this });
            }
        }

        private void ConnectCallback(IAsyncResult result)
        {
            connectEvent.Set();
            var arg = result.AsyncState as TcpSocketEventArgs;
            try
            {
                arg.Client.Socket.EndConnect(result);
                Stamp = DateTime.Now;
                Server.OnClientConnect(arg);
            }
            catch (Exception ex)
            {
                Server.OnDataException(new TcpExceptionEventArgs(arg, ex));
            }
        }

        public void Disconnect(bool reuse)
        {
            disconnectEvent.WaitOne();
            if (Socket.Connected)
            {
                Debug.WriteLine("Disconnect");
                Socket.Shutdown(SocketShutdown.Both);
                Socket.BeginDisconnect(reuse, DisconnectCallback, new TcpSocketEventArgs { Client = this });
            }
            else
                disconnectEvent.Set();
        }

        private void DisconnectCallback(IAsyncResult result)
        {
            var arg = result.AsyncState as TcpSocketEventArgs;
            try
            {
                Socket.EndDisconnect(result);
                disconnectEvent.Set();
                loadEvent.Set();
                sendEvent.Set();
                Server.OnClientDisconect(arg);
                socket.Close();
            }
            catch (Exception ex)
            {
                Server.OnDataException(new TcpExceptionEventArgs(arg, ex));
            }
        }

        public void Dispose()
        {
            Disconnect(false);
            connectEvent?.Dispose();
            disconnectEvent?.Dispose();
            loadEvent?.Dispose();
            sendEvent?.Dispose();
        }
    }
}
