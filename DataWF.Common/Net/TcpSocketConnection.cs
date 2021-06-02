using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class TcpSocketConnection : SocketConnection
    {
        private Socket socket;

        public TcpSocketConnection()
        { }

        public Socket Socket
        {
            get { return socket; }
            set
            {
                socket = value;
                if (socket != null)
                {
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    if (socket.RemoteEndPoint is IPEndPoint point)
                    {
                        Point = point;
                        Address = socket.RemoteEndPoint.ToUrl();
                    }
                }
            }
        }

        public override Uri Address
        {
            get => base.Address;
            set
            {
                base.Address = value;
                Point = value?.ToEndPoint();
            }
        }

        public IPEndPoint Point { get; set; }

        public IPEndPoint LocalPoint => socket == null ? null : (IPEndPoint)socket.LocalEndPoint;

        public override bool Connected => Socket?.Connected ?? false;


        protected override async Task<int> SendPart(SocketStreamArgs arg, int read)
        {
            //Debug.WriteLine($"TcpClient {Point} Send {sended}");
#if NETSTANDARD2_0
            return await Task.Factory.FromAsync<int>(Socket.BeginSend(arg.Buffer.Array, 0, read, SocketFlags.None, null, arg), Socket.EndSend);
#else
            return await Socket.SendAsync(arg.Buffer.Slice(0, read), SocketFlags.None);
#endif
        }

        protected override async Task SendFin(SocketStreamArgs arg)
        {
            //Socket.NoDelay = true;
#if NETSTANDARD2_0
            await Task.Factory.FromAsync<int>(Socket.BeginSend(fin, 0, finLength, SocketFlags.None, null, arg), Socket.EndSend);
#else
            await Socket.SendAsync(fin, SocketFlags.None);
#endif
            //Socket.NoDelay = false;
        }



        protected override async Task<int> ReceivePart(SocketStreamArgs arg)
        {
#if NETSTANDARD2_0
            return await Task.Factory.FromAsync(Socket.BeginReceive(arg.Buffer.Array, 0, arg.BufferSize, SocketFlags.None, null, arg), Socket.EndReceive);
#else
            var memory = arg.Pipe.Writer.GetMemory(arg.BufferSize);
            return await Socket.ReceiveAsync(memory, SocketFlags.None);
#endif
        }

        public override async ValueTask Connect()
        {
            if (Point == null)
                throw new Exception("Point not Specified");
            if (Socket == null)
            {
                Socket = new Socket(Point.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Socket.Bind(((TcpSocketService)Server).Point);
            }
            if (!Socket.Connected)
            {
                var arg = new SocketConnectionArgs(this);
                try
                {
                    Debug.WriteLine($"TcpClient {Point} Connect to {Point}");
                    await Task.Factory.FromAsync(Socket.BeginConnect(Point, null, arg), Socket.EndDisconnect);
                    Stamp = DateTime.UtcNow;
                    _ = Server.OnClientConnect(arg);
                }
                catch (Exception ex)
                {
                    Server.OnDataException(new SocketExceptionArgs(arg, ex));
                    throw;
                }
            }
        }

        public override async ValueTask Disconnect()
        {
            if (Socket?.Connected ?? false)
            {
                var arg = new SocketConnectionArgs(this);
                try
                {
                    Debug.WriteLine($"TcpClient {Point} Disconnect");
                    Socket.Shutdown(SocketShutdown.Both);
                    await Task.Factory.FromAsync(Socket.BeginDisconnect(true, null, arg), Socket.EndDisconnect);
                    Socket.Close();
                    Socket.Dispose();
                    Socket = null;
                    receiveEvent.Set();
                    sendEvent.Set();
                    _ = Server.OnClientDisconect(arg);
                }
                catch (Exception ex)
                {
                    Server.OnDataException(new SocketExceptionArgs(arg, ex));
                }
            }
        }

        public override void Dispose()
        {
            if (!disposed)
            {
                Debug.WriteLine($"TcpClient {Point} Dispose");
                if (Connected)
                {
                    Debug.WriteLine($"TcpClient {Point} Dispose Disconnect");
                    Socket.Shutdown(SocketShutdown.Both);
                    Socket.Disconnect(true);
                    Socket.Close();
                    Socket.Dispose();
                    Socket = null;
                    receiveEvent.Set();
                    sendEvent.Set();

                }
                base.Dispose();
            }
        }

        public override void OnTimeOut()
        {
            if (Socket.Poll(5000, SelectMode.SelectRead) && (Socket.Available == 0))
            {
                Server.OnClientTimeout(new SocketConnectionArgs(this));

                Dispose();
            }
            else
            {
                Stamp = DateTime.UtcNow;
            }
        }
    }
}
