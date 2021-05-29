using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface ISocketService : IDisposable
    {
        Uri Address { get; set; }
        IEnumerable<ISocketConnection> Clients { get; }
        SocketCompressionMode Compression { get; set; }
        bool LogEvents { get; set; }
        bool OnLine { get; }
        TimeSpan TransferTimeOut { get; set; }
        int BufferSize { get; set; }

        event EventHandler<SocketConnectionArgs> ClientConnect;
        event EventHandler<SocketConnectionArgs> ClientDisconnect;
        event EventHandler<SocketConnectionArgs> ClientTimeout;
        event EventHandler<SocketExceptionArgs> DataException;
        event EventHandler<SocketStreamArgs> DataLoad;
        event EventHandler<SocketStreamArgs> DataSend;
        event EventHandler Started;
        event EventHandler Stopped;

        ValueTask<ISocketConnection> CreateClient(Uri address);
        ValueTask<ISocketConnection> CreateClient(object socket);
        ISocketConnection GetConnection(Uri address);
        ISocketConnection GetConnection(string name);
        void Remove(ISocketConnection client);
        void StartListener(int backlog);
        void StopListener();
        void WaitAll();
        object OnSended(SocketStreamArgs args);
        void OnReceiveStart(SocketStreamArgs args);
        object OnReceiveFinish(SocketStreamArgs args);
        void OnDataException(SocketExceptionArgs args);
        object OnClientDisconect(SocketConnectionArgs args);
        object OnClientConnect(SocketConnectionArgs args);
    }
}