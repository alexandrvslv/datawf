using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public abstract class LoggedClient<T, K, L> : Client<T, K>, ILoggedClient<T, L>
        where T : class, new()
        where K : struct
        where L : class, new()
    {
        public LoggedClient(ActionInvoker<T, K?> idInvoker, ActionInvoker<T, int?> typeInvoker, int typeId = 0)
            : base(idInvoker, typeInvoker, typeId)
        { }

        public abstract Task<List<L>> GetItemLogsAsync(K id, ProgressToken progressToken);

        public Task<List<L>> GetItemLogsAsync(object id, ProgressToken progressToken)
            => GetItemLogsAsync((K)id, progressToken);

        public async Task<IEnumerable> GetItemLogsAsync(object id)
            => await GetItemLogsAsync(id, ProgressToken.None);

        public abstract Task<List<L>> GetLogsAsync(string filter, ProgressToken progressToken);

        public async Task<IEnumerable> GetLogsAsync(string filter)
            => await GetLogsAsync(filter, ProgressToken.None);

        public abstract Task<T> RedoLogAsync(long logId, ProgressToken progressToken);

        public async Task<object> RedoLogAsync(long logId)
            => await RedoLogAsync(logId, ProgressToken.None);

        public abstract Task<bool> RemoveLogAsync(long logId, ProgressToken progressToken);

        public async Task<bool> RemoveLogAsync(long logId)
            => await RemoveLogAsync(logId, ProgressToken.None);

        public abstract Task<T> UndoLogAsync(long logId, ProgressToken progressToken);

        public async Task<object> UndoLogAsync(long logId)
            => await UndoLogAsync(logId, ProgressToken.None);
    }
}