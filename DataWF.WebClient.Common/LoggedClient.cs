﻿using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public abstract class LoggedClient<T, K, L> : Client<T, K>, ILoggedClient<T, L>
        where T : class, new()
        where K : struct
        where L : class, new()
    {
        public LoggedClient(Invoker<T, K?> idInvoker, Invoker<T, int?> typeInvoker, int typeId = 0)
            : base(idInvoker, typeInvoker, typeId)
        { }

        public abstract Task<List<L>> GetItemLogsAsync(K id, HttpJsonSettings settings, ProgressToken progressToken);

        public Task<List<L>> GetItemLogsAsync(object id, HttpJsonSettings settings, ProgressToken progressToken)
            => GetItemLogsAsync((K)id, settings, progressToken);

        public async Task<IEnumerable> GetItemLogsAsync(object id)
            => await GetItemLogsAsync(id, HttpJsonSettings.OnlyReferenced, ProgressToken.None);

        public abstract Task<List<L>> GetLogsAsync(string filter, HttpJsonSettings settings, ProgressToken progressToken);

        public async Task<IEnumerable> GetLogsAsync(string filter)
            => await GetLogsAsync(filter, HttpJsonSettings.OnlyReferenced, ProgressToken.None);

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