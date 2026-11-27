using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DataWF
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
            return await Socket.SendAsync(arg.Buffer.Slice(0, read), SocketFlags.None);
        }

        protected override async Task SendFin(SocketStreamArgs arg)
        {
            //Socket.NoDelay = true;
            await Socket.SendAsync(fin, SocketFlags.None);
            //Socket.NoDelay = false;
        }



        protected override async Task<int> ReceivePart(SocketStreamArgs arg)
        {
            var memory = arg.Pipe.Writer.GetMemory(arg.BufferSize);
            return await Socket.ReceiveAsync(memory, SocketFlags.None);
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
