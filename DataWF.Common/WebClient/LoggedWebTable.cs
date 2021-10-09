using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public abstract class LoggedWebTable<T, K, L> : WebTable<T, K>, ILoggedWebTable<T, L>
        where T : class, new()
        where K : struct
        where L : class, new()
    {
        public LoggedWebTable(IInvoker<T, K> idInvoker, IInvoker<T, int> typeInvoker, int typeId = 0)
            : base(idInvoker, typeInvoker, typeId)
        { }

        public abstract Task<List<L>> GetItemLogs(K id, ProgressToken progressToken);

        public Task<List<L>> GetItemLogs(object id, ProgressToken progressToken) => GetItemLogs((K)id, progressToken);

        public abstract Task<List<L>> GetLogs(string filter, ProgressToken progressToken);

        public abstract Task<T> RedoLog(long logId, ProgressToken progressToken);

        public abstract Task<bool> RemoveLog(long logId, ProgressToken progressToken);

        public abstract Task<T> UndoLog(long logId, ProgressToken progressToken);


        async Task<IEnumerable> ILoggedWebTable.GetItemLogs(object id, ProgressToken progressToken)//new HttpJsonSettings(HttpJsonKeys.Refed, 2)
            => await GetItemLogs(id, progressToken);
        async Task<IEnumerable> ILoggedWebTable.GetLogs(string filter, ProgressToken progressToken)//new HttpJsonSettings(HttpJsonKeys.Refed, 2)
            => await GetLogs(filter, progressToken);
        async Task<object> ILoggedWebTable.RedoLog(long logId, ProgressToken progressToken)
             => await RedoLog(logId, progressToken);
        async Task<object> ILoggedWebTable.UndoLog(long logId, ProgressToken progressToken)
            => await UndoLog(logId, progressToken);
    }
}