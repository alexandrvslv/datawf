using System;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IWebNotifyClient : IDisposable
    {
        event EventHandler<WebNotifyClientEventArgs> OnReceiveMessage;

        Task Close();
        void Listen();
        Task RegisterNotify(Uri uri, string autorization);
    }
}