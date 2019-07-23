using System;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IWebNotifyClient : IDisposable
    {
        event EventHandler<WebNotifyClientEventArgs> OnReceiveMessage;
        event EventHandler<ExceptionEventArgs> OnError;
        event EventHandler<EventArgs> OnClose;

        bool CloseRequest { get; }
        Task Close();
        void Listen();
        Task RegisterNotify(Uri uri, string autorization);
    }
}