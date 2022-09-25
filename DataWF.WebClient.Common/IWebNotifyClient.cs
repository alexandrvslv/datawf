using System;
using System.IO;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IWebNotifyClient : IDisposable
    {
        event EventHandler<WebNotifyClientEventArgs> OnReceiveMessage;
        event EventHandler<ExceptionEventArgs> OnError;
        event EventHandler<EventArgs> OnClose;
        event EventHandler<EventArgs> OnOpen;

        bool CloseRequest { get; }
        Task Close();
        void Listen();
        Task Send(byte[] data);
        Task Send(Stream data);
        Task RegisterNotify(Uri uri, string autorization, string email);
    }
}