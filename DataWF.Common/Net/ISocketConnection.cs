using System;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface ISocketConnection: IDisposable
    {
        bool Connected { get; }
        string Name { get; }
        ISocketService Server { get; set; }
        DateTime Stamp { get; set; }
        Uri Address { get; set; }
        Func<SocketStreamArgs, ValueTask> ReceiveStart { get; set; }
        ValueTask Connect();
        ValueTask Disconnect();
        Pipe GetPipe();
        Task ListenerLoop();
        Task<bool> Send(byte[] data);
        Task<bool> Send(Stream stream);
        Task<bool> Send(SocketStreamArgs arg);
        Task<bool> SendT<T>(T element);
        void WaitAll();
        void OnTimeOut();
    }
}