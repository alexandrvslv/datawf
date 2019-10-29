using System;
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
        void Send(byte[] data);
        Task RegisterNotify(Uri uri, string autorization);
    }
}