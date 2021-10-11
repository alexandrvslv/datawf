using DataWF.Common;
using DataWF.Data;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace DataWF.WebService.Common
{
    public interface IWebNotifyService
    {
        public IDBProvider Provider { get; }

        WebNotifyConnection Register(WebSocket socket, IUserIdentity userIdentity, string v);
        Task ListenAsync(WebNotifyConnection connection);
        IEnumerable<WebNotifyConnection> GetConnections();
        void Start();
        void Stop();
    }
}