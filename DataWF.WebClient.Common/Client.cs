using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
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
        private ICrudClient baseClient;
        private ConcurrentDictionary<string, LoadProgress<T>> loadQueue = new ConcurrentDictionary<string, LoadProgress<T>>(StringComparer.OrdinalIgnoreCase);
        private SemaphoreSlim getActionSemaphore;

        public Client(Invoker<T, K?> idInvoker, Invoker<T, int?> typeInvoker, int typeId = 0)
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

        public Invoker<T, K?> IdInvoker { get; }

        public Invoker<T, int?> TypeInvoker { get; }

        public int TypeId { get; }

        public Type ItemType { get { return typeof(T); } }

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

        public T NewLoadItem()
        {
            var newItem = new T();
            if (newItem is ISynchronized synched)
            {
                synched.SyncStatus = SynchronizedStatus.Load;
            }

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
            if (item is T)
            {
                return AddDownloads((K)id, (T)item);
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
            var item = downloads?.GetOrAdd(id, newItem) ?? GetBaseClient()?.AddDownloads(id, newItem) as T;
            if (item == null)
            {
                item = NewLoadItem();
                SetDownloads((K)id, item);
            }
            return item;
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
            var type = typeof(T).BaseType;
            var result = (ICrudClient)null;
            while (type != typeof(object))
            {
                var client = Provider.GetClient(type);
                if (client != null)
                    result = client;
                type = type.BaseType;
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

        public IEnumerable<object> GetChanges()
        {
            return Items.Cast<ISynchronized>().Where(p =>
                        p != null
                        && (p.SyncStatus == SynchronizedStatus.New
                        || p.SyncStatus == SynchronizedStatus.Edit));
        }

        public object Select(object id)
        {
            if (id == null)
                return null;
            return Select((K)id);
        }

        public virtual T Select(K id)
        {
            return Items.SelectOne(IdInvoker.Name, (K?)id) ?? GetDownloads(id);
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

        public Task<List<T>> LoadAsync()
        {
            return LoadAsync(string.Empty, HttpJsonSettings.Default, ProgressToken.None);
        }

        public async Task<List<T>> LoadAsync(string filter, HttpJsonSettings settings, ProgressToken progressToken)
        {
            return await Load(filter, settings, progressToken).Task.ConfigureAwait(false);
        }

        public LoadProgress<T> Load(string filter, HttpJsonSettings settings, ProgressToken progressToken)
        {
            filter = filter ?? string.Empty;
            if (!loadQueue.TryGetValue(filter, out var loadTask) || loadTask.Token.IsCancelled)
            {
                loadQueue[filter] = loadTask = new LoadProgress<T>(filter, progressToken);
                loadTask.Task = string.IsNullOrEmpty(filter)
                    ? LoadAsync(settings, loadTask.Token)
                    : SearchAsync(filter, settings, loadTask.Token);
            }

            return loadTask;
        }

        private async Task<List<T>> LoadAsync(HttpJsonSettings settings, ProgressToken token)
        {
            var list = await GetAsync(settings, token);
            IsSynchronized = true;
            return list;
        }

        public virtual Task<List<T>> GetAsync(HttpJsonSettings settings, ProgressToken progressToken) => Task.FromResult<List<T>>(null);

        public async Task<IEnumerable> GetAsync() => await GetAsync(HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<List<T>> SearchAsync(string filter, HttpJsonSettings settings, ProgressToken progressToken) => Task.FromResult<List<T>>(null);

        public async Task<IEnumerable> SearchAsync(string filter) => await SearchAsync(filter, HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<T> GetAsync(K id, HttpJsonSettings settings, ProgressToken progressToken) => Task.FromResult(default(T));

        public Task<T> GetAsync(object id, HttpJsonSettings settings, ProgressToken progressToken) => GetAsync((K)id, settings, progressToken);

        public async Task<object> GetAsync(object id) => await GetAsync((K)id, HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<T> CopyAsync(K id, HttpJsonSettings settings, ProgressToken progressToken) => Task.FromResult(default(T));

        public Task<T> CopyAsync(object id, HttpJsonSettings settings, ProgressToken progressToken) => CopyAsync((K)id, settings, progressToken);

        public async Task<object> CopyAsync(object id) => await CopyAsync((K)id, HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<T> PutAsync(T value, HttpJsonSettings settings, ProgressToken progressToken) => Task.FromResult(value);

        public Task<T> PutAsync(object value, HttpJsonSettings settings, ProgressToken progressToken) => PutAsync((T)value, settings, progressToken);

        public async Task<object> PutAsync(object value) => await PutAsync((T)value, HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<T> PostAsync(T value, HttpJsonSettings settings, ProgressToken progressToken) => Task.FromResult(value);

        public Task<T> PostAsync(object value, HttpJsonSettings settings, ProgressToken progressToken) => PostAsync((T)value, settings, progressToken);

        public async Task<object> PostAsync(object value) => await PostAsync((T)value, HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<bool> DeleteAsync(K id, HttpJsonSettings settings, ProgressToken progressToken) => Task.FromResult(true);

        public Task<bool> DeleteAsync(object id, HttpJsonSettings settings, ProgressToken progressToken) => DeleteAsync((K)id, settings, progressToken);

        public Task<bool> DeleteAsync(object id) => DeleteAsync((K)id, HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<object> GenerateIdAsync(HttpJsonSettings settings, ProgressToken progressToken) => Task.FromResult<object>(null);

        public Task<object> GenerateId() => GenerateIdAsync(HttpJsonSettings.Default, ProgressToken.None);

        public virtual Task<T> MergeAsync(K id, List<string> ids, HttpJsonSettings settings, ProgressToken progressToken) => Task.FromResult<T>(null);

        public Task<T> MergeAsync(T item, List<string> ids, HttpJsonSettings settings, ProgressToken progressToken)
            => MergeAsync(IdInvoker.GetValue(item).Value, ids, settings, ProgressToken.None);

        public async Task<object> MergeAsync(object id, List<string> ids) => await MergeAsync((K)id, ids, HttpJsonSettings.Default, ProgressToken.None);
    }

    public class LoadProgress<T>
    {
        public LoadProgress(string filter, ProgressToken token)
        {
            Filter = filter;
            Token = token;
        }
        public ProgressToken Token { get; }
        public Task<List<T>> Task { get; set; }
        public string Filter { get; internal set; }
    }
}