using DataWF.WebClient.Common;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public abstract partial class Client<T, K> : ClientBase, ICrudClient<T>
        where T : class, new()
        where K : struct
    {
        private readonly ConcurrentDictionary<K, T> downloads;
        private readonly ConcurrentDictionary<string, LoadProgress<T>> loadQueue = new ConcurrentDictionary<string, LoadProgress<T>>(StringComparer.OrdinalIgnoreCase);
        private ICrudClient baseClient;
        private SemaphoreSlim getActionSemaphore;

        public Client(IInvoker<T, K?> idInvoker, IInvoker<T, int?> typeInvoker, int typeId = 0)
        {
            IdInvoker = idInvoker;
            Items.Indexes.Concurrent = true;
            Items.Indexes.Add(IdInvoker);
            Items.CollectionChanged += OnItemsCollectionChanged;
            TypeInvoker = typeInvoker;
            TypeId = typeId;
#if NETSTANDARD2_0
            Converter = new NewtonJsonClientConverter<T, K>(this);
#else
            Converter = new SystemJsonClientConverter<T, K>(this);
#endif
            if (typeId == 0)
            {
                downloads = new ConcurrentDictionary<K, T>();
            }
        }

        public IClientConverter Converter { get; }

        public IInvoker<T, K?> IdInvoker { get; }

        public IInvoker<T, int?> TypeInvoker { get; }

        public int TypeId { get; }

        public Type ItemType { get { return typeof(T); } }

        IList ICrudClient.Items => Items;

        public ChangeableList<T> Items { get; set; } = new ChangeableList<T> { AsyncNotification = true };

        public bool IsSynchronized
        {
            get => Items.IsSynchronized;
            set
            {
                Items.IsSynchronized = value;
                Status = value ? ClientStatus.Compleate : ClientStatus.None;
            }
        }

        public override void ClearCache()
        {
            IsSynchronized = false;
            Items.Clear();
            loadQueue.Clear();
            base.ClearCache();
        }

        public virtual bool ClearLoadCache(string filter)
        {
            return loadQueue.TryRemove(filter, out _);
        }

        public T NewLoadItem(K p)
        {
            var newItem = new T();
            if (newItem is ISynchronized synched)
            {
                synched.SyncStatus = SynchronizedStatus.Load;
            }
            IdInvoker.SetValue(newItem, p);
            return newItem;
        }

        public bool RemoveDownloads(object id)
        {
            return RemoveDownloads((K)id);
        }

        public bool RemoveDownloads(K id)
        {
            return downloads != null
                ? downloads.TryRemove(id, out _)
                : (GetBaseClient()?.RemoveDownloads(id) ?? false);
        }

        public object AddDownloads(object id, object item)
        {
            if (item is T typed)
            {
                return AddDownloads((K)id, typed);
            }
            else
            {
                return AddDownloads((K)id, (Func<K, T>)item);
            }
        }

        public T AddDownloads(K id, T item)
        {
            if (downloads != null)
            {
                return downloads.GetOrAdd(id, item);
            }
            else
            {
                var loadItem = GetBaseClient()?.AddDownloads(id, item) ?? item;
                if (loadItem is T typedItem)
                {
                    return typedItem;
                }
                else
                {
                    GetBaseClient().SetDownloads(id, item);
                    return item;
                }
            }
        }

        public T AddDownloads(K id, Func<K, T> newItem)
        {
            return downloads?.GetOrAdd(id, newItem) ?? GetBaseClient()?.AddDownloads(id, newItem) as T;
        }

        public object GetDownloads(object id)
        {
            return GetDownloads((K)id);
        }

        public T GetDownloads(K id)
        {
            return downloads != null
                 ? downloads.TryGetValue(id, out var item) ? item : null
                 : GetBaseClient()?.GetDownloads(id) as T;
        }

        public void SetDownloads(object id, object item)
        {
            SetDownloads((K)id, (T)item);
        }

        public void SetDownloads(K id, T item)
        {
            if (downloads != null)
            {
                downloads[id] = item;
            }
            else
            {
                GetBaseClient()?.SetDownloads(id, item);
            }
        }

        internal T SelectBase(object id)
        {
            return GetBaseClient()?.Select(id) as T;
        }

        public ICrudClient GetBaseClient()
        {
            if (TypeId == 0 || baseClient != null)
                return baseClient;
            var result = Provider.GetClient(typeof(T).BaseType);
            while (result != null && result.TypeId != 0 && result.ItemType.BaseType != null)
            {
                result = Provider.GetClient(result.ItemType.BaseType);
            }
            return baseClient = result;
        }

        public object NewItem()
        {
            return new T();
        }

        protected virtual void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                OnAdded(e.NewItems);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                OnRemoved(e.OldItems);
            }
        }

        protected virtual void OnAdded(IList items)
        { }

        protected virtual void OnRemoved(IList items)
        { }

        public override bool Add(object item)
        {
            return Add((T)item);
        }

        public virtual bool Add(T item)
        {
            var added = Items.Add(item) > -1;
            GetBaseClient()?.Add(item);
            return added;
        }

        public override bool Remove(object item)
        {
            return Remove((T)item);
        }

        public virtual bool Remove(T item)
        {
            var removed = Items.Remove(item);
            if (!removed)
            {
                removed = RemoveDownloads((K)IdInvoker.GetValue(item));
            }
            GetBaseClient()?.Remove(item);
            return removed;
        }

        public void RemoveById(object id)
        {
            var item = Select(id);

            if (item != null)
            {
                if (item.GetType() != ItemType)
                {
                    var client = Provider.GetClient(item.GetType());
                    client?.Remove(item);
                    return;
                }
                Remove(item);
            }
        }

        public object ParseId(object id)
        {
            return Helper.Parse(id, IdInvoker.DataType);
        }

        IEnumerable ICrudClient.GetChanges()
        {
            return Items.GetChanged();
        }

        public ICollection<T> GetChanges()
        {
            return Items.GetChanged();
        }

        public object Select(object id)
        {
            if (id == null)
                return null;
            return Select((K)id);
        }

        public virtual T SelectNoDownloads(K id)
        {
            return Items.SelectOne<K?>(IdInvoker.Name, (K?)id);
        }

        public virtual T Select(K id)
        {
            return Items.SelectOne<K?>(IdInvoker.Name, (K?)id) ?? GetDownloads(id);
        }

        public virtual T Get(object id)
        {
            return Get((K)id);
        }

        public async virtual Task<T> Get(T item)
        {
            if (item is ISynchronized synched)
                synched.SyncStatus = SynchronizedStatus.Load;
            return await GetAsync(IdInvoker.GetValue(item), HttpJsonSettings.Default, ProgressToken.None).ConfigureAwait(false);
        }

        public async virtual Task<bool> Delete(T item)
        {
            var result = await DeleteAsync(IdInvoker.GetValue(item), HttpJsonSettings.Default, ProgressToken.None).ConfigureAwait(false);
            if (result)
            {
                Remove(item);
            }
            return result;
        }

        public virtual T Get(K id)
        {
            return Get(id, null);
        }

        public virtual T Get(K id, Action<T> loadAction)
        {
            var item = Select(id);
            if (item == null)
            {
                item = new T();
                if (item is IPrimaryKey keyed)
                    keyed.PrimaryKey = id;
                if (item is ISynchronized synched)
                    synched.SyncStatus = SynchronizedStatus.Load;
                item = AddDownloads(id, item);
                _ = GetAction(id, loadAction).ConfigureAwait(false);
            }
            return item;
        }

        private async ValueTask GetAction(K id, Action<T> loadAction)
        {
            if (getActionSemaphore == null)
                getActionSemaphore = new SemaphoreSlim(2);
            await getActionSemaphore.WaitAsync();
            try
            {
                Debug.WriteLine($"Client.Get {typeof(T)} {id}");
                var result = await GetAsync(id, HttpJsonSettings.Default, ProgressToken.None).ConfigureAwait(false);
                loadAction?.Invoke(result);
            }
            finally
            {
                getActionSemaphore.Release();
            }
        }

        public string GetFilePath(IFileModel fileModel)
        {
            return GetFilePath(fileModel, $"/api/{typeof(T).Name}/DownloadFile/{{id}}");
        }

        public Task<List<T>> LoadAsync(HttpPageSettings pages)
        {
            return LoadAsync(string.Empty, pages, HttpJsonSettings.Default, ProgressToken.None);
        }

        public async Task<List<T>> LoadAsync(string filter, HttpPageSettings pages, HttpJsonSettings json, ProgressToken progressToken)
        {
            return await Load(filter, pages, json, progressToken).Task.ConfigureAwait(false);
        }

        public LoadProgress<T> Load(string filter, HttpPageSettings pages, HttpJsonSettings json, ProgressToken progressToken)
        {
            filter = filter ?? string.Empty;
            if (!loadQueue.TryGetValue(filter, out var loadTask)
                || loadTask.Task.Status == TaskStatus.Faulted
                || loadTask.Task.Status == TaskStatus.Canceled)
            {
                loadTask = new LoadProgress<T>(filter, pages, json, progressToken);
                if (pages == null)
                {
                    loadQueue[filter] = loadTask;
                }
                loadTask.Task = string.IsNullOrEmpty(filter)
                    ? LoadAsync(pages, json, loadTask.Token)
                    : SearchAsync(filter, pages, json, loadTask.Token);
            }

            return loadTask;
        }

        private async Task<List<T>> LoadAsync(HttpPageSettings pages, HttpJsonSettings json, ProgressToken token)
        {
            var result = await GetAsync(pages, json, token);
            IsSynchronized = true;
            return result;
        }

        public virtual Task<List<T>> GetAsync(HttpPageSettings pages, HttpJsonSettings json, ProgressToken progressToken) => Task.FromResult<List<T>>(null);

        public async Task<IEnumerable> GetAsync(HttpPageSettings pages) => await GetAsync(pages, HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<List<T>> SearchAsync(string filter, HttpPageSettings pages, HttpJsonSettings json, ProgressToken progressToken) => Task.FromResult<List<T>>(null);

        public async Task<IEnumerable> SearchAsync(string filter, HttpPageSettings pages) => await SearchAsync(filter, pages, HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<T> GetAsync(K id, HttpJsonSettings json, ProgressToken progressToken) => Task.FromResult(default(T));

        public Task<T> GetAsync(object id, HttpJsonSettings json, ProgressToken progressToken) => GetAsync((K)id, json, progressToken);

        public async Task<object> GetAsync(object id) => await GetAsync((K)id, HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<T> CopyAsync(K id, HttpJsonSettings json, ProgressToken progressToken) => Task.FromResult(default(T));

        public Task<T> CopyAsync(object id, HttpJsonSettings json, ProgressToken progressToken) => CopyAsync((K)id, json, progressToken);

        public async Task<object> CopyAsync(object id) => await CopyAsync((K)id, HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<T> PutAsync(T value, HttpJsonSettings json, ProgressToken progressToken) => Task.FromResult(value);

        public Task<T> PutAsync(object value, HttpJsonSettings json, ProgressToken progressToken) => PutAsync((T)value, json, progressToken);

        public async Task<object> PutAsync(object value) => await PutAsync((T)value, HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<T> PostAsync(T value, HttpJsonSettings json, ProgressToken progressToken) => Task.FromResult(value);

        public Task<T> PostAsync(object value, HttpJsonSettings json, ProgressToken progressToken) => PostAsync((T)value, json, progressToken);

        public async Task<object> PostAsync(object value) => await PostAsync((T)value, HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<List<T>> PostPackageAsync(List<T> value, HttpJsonSettings json, ProgressToken progressToken) => Task.FromResult(value);

        public async Task<IList> PostPackageAsync(IList value) => await PostPackageAsync((List<T>)value, HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<bool> DeleteAsync(K id, HttpJsonSettings json, ProgressToken progressToken) => Task.FromResult(true);

        public Task<bool> DeleteAsync(object id, HttpJsonSettings json, ProgressToken progressToken) => DeleteAsync((K)id, json, progressToken);

        public Task<bool> DeleteAsync(object id) => DeleteAsync((K)id, HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<object> GenerateIdAsync(HttpJsonSettings json, ProgressToken progressToken) => Task.FromResult<object>(null);

        public Task<object> GenerateId() => GenerateIdAsync(HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<T> MergeAsync(K id, List<string> ids, HttpJsonSettings json, ProgressToken progressToken) => Task.FromResult<T>(null);

        public Task<T> MergeAsync(T item, List<string> ids, HttpJsonSettings json, ProgressToken progressToken)
            => MergeAsync(IdInvoker.GetValue(item).Value, ids, json, ProgressToken.None);

        public async Task<object> MergeAsync(object id, List<string> ids) => await MergeAsync((K)id, ids, HttpJsonSettings.Default, ProgressToken.None);

        public IEnumerable<object> SearchByValue(IInvoker valueInvoker, object value)
        {
            var index = Items.Indexes.Add(valueInvoker);
            //if (result == null)
            //{
            //    var parameter = new QueryParameter<T> { Invoker = valueInvoker, Value = value };
            //    _ = LoadAsync(parameter.Format(false), null, HttpJsonSettings.Default, ProgressToken.None);
            //}
            return index.Scan(CompareType.Equal, value).Cast<object>();
        }
    }
}