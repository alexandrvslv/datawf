using DataWF.Common;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace DataWF.WebService.Common
{
    public interface IWebNotifyService
    {
        WebNotifyConnection Register(WebSocket socket, IUserIdentity userIdentity, string v);
        Task ListenAsync(WebNotifyConnection connection);
        IEnumerable<WebNotifyConnection> GetConnections();
        void Start();
        void Stop();
    }
}