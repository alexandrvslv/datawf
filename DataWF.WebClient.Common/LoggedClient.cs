using DataWF.WebClient.Common;
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
        public LoggedClient(IInvoker<T, K> idInvoker, IInvoker<T, int> typeInvoker, int typeId = 0)
            : base(idInvoker, typeInvoker, typeId)
        { }

        public abstract Task<List<L>> GetItemLogsAsync(K id, HttpPageSettings list, HttpJsonSettings settings, ProgressToken progressToken);

        public Task<List<L>> GetItemLogsAsync(object id, HttpPageSettings list, HttpJsonSettings settings, ProgressToken progressToken)
            => GetItemLogsAsync((K)id, list, settings, progressToken);

        public async Task<IEnumerable> GetItemLogsAsync(object id, HttpPageSettings list)
            => await GetItemLogsAsync(id, list, new HttpJsonSettings(HttpJsonKeys.Refed, 2), ProgressToken.None);

        public abstract Task<List<L>> GetLogsAsync(string filter, HttpPageSettings list, HttpJsonSettings settings, ProgressToken progressToken);

        public async Task<IEnumerable> GetLogsAsync(string filter, HttpPageSettings list)
            => await GetLogsAsync(filter, list, new HttpJsonSettings(HttpJsonKeys.Refed, 2), ProgressToken.None);

        public abstract Task<T> RedoLogAsync(long logId, HttpJsonSettings settings, ProgressToken progressToken);

        public async Task<object> RedoLogAsync(long logId)
            => await RedoLogAsync(logId, HttpJsonSettings.Default, ProgressToken.None);

        public abstract Task<bool> RemoveLogAsync(long logId, HttpJsonSettings settings, ProgressToken progressToken);

        public async Task<bool> RemoveLogAsync(long logId)
            => await RemoveLogAsync(logId, HttpJsonSettings.Default, ProgressToken.None);

        public abstract Task<T> UndoLogAsync(long logId, HttpJsonSettings settings, ProgressToken progressToken);

        public async Task<object> UndoLogAsync(long logId)
            => await UndoLogAsync(logId, HttpJsonSettings.Default, ProgressToken.None);
    }
}