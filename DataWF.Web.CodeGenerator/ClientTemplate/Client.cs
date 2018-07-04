using DataWF.Common;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Web.Client
{
    public abstract partial class Client<T, K> : ClientBase, ICRUDClient
    {
        public abstract Task<List<T>> GetAsync();

        public abstract Task<List<T>> GetAsync(CancellationToken cancellationToken);

        Task ICRUDClient.GetAsync() { return GetAsync(); }

        public abstract Task<T> PutAsync(T value);

        public abstract Task<T> PutAsync(T value, CancellationToken cancellationToken);

        public Task PutAsync(object value) { return PutAsync((K)value); }

        public abstract Task<T> PostAsync(T value);

        public abstract Task<T> PostAsync(T value, CancellationToken cancellationToken);

        public Task PostAsync(object value) { return PostAsync((K)value); }

        public abstract Task<List<T>> FindAsync(string filter);

        public abstract Task<List<T>> FindAsync(string filter, CancellationToken cancellationToken);

        Task ICRUDClient.FindAsync(string filter) { return FindAsync(filter); }

        public abstract Task<T> GetAsync(K id);

        public abstract Task<T> GetAsync(K id, CancellationToken cancellationToken);

        public Task GetAsync(object id) { return GetAsync((K)id); }

        public abstract Task<bool> DeleteAsync(K id);

        public abstract Task<bool> DeleteAsync(K id, CancellationToken cancellationToken);

        public Task DeleteAsync(object id) { return DeleteAsync((K)id); }
    }
}