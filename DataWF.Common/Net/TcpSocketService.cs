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
    public class TcpSocketService : SocketService
    {
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

                    Debug.WriteLine($"TcpServer {Point} Wait Connection");
                    socket = await Task.Factory.FromAsync<Socket>(Socket.BeginAccept(socket, 0, null, null), Socket.EndAccept);

                    Debug.WriteLine($"TcpServer {Point} Accepted: {socket.RemoteEndPoint}");
                    var arg = new SocketConnectionArgs(await CreateConnection(socket));
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

        public override async ValueTask<ISocketConnection> CreateConnection(Uri address)
        {
            var client = GetConnection(address);
            if (client == null)
            {
                client = new TcpSocketConnection { Server = this, Address = address };
                await client.Connect();
            }
            return client;
        }

        public override ValueTask<ISocketConnection> CreateConnection(object socket)
        {
            if (!(socket is Socket tcpSocket))
            {
                throw new ArgumentException($"Expect {typeof(Socket)} type!", nameof(socket));
            }
            var client = new TcpSocketConnection { Server = this, Socket = tcpSocket };
            return new ValueTask<ISocketConnection>(client);
        }

       
    }
}
