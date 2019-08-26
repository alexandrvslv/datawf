using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{

    public abstract partial class Client<T, K> : ClientBase, ICRUDClient<T>
        where T : class, new()
        where K : struct
    {
        public Client(Invoker<T, K?> idInvoker, Invoker<T, int?> typeInvoker, int typeId = 0)
        {
            IdInvoker = idInvoker;
            Items.Indexes.Add(IdInvoker);
            TypeInvoker = typeInvoker;
            TypeId = typeId;
            SerializationInfo = new TypeSerializationInfo(typeof(T));
            if (typeId == 0)
                downloads = new ConcurrentDictionary<K, T>();
        }
        private ConcurrentDictionary<K, T> downloads;
        private ICRUDClient baseClient;
        private LoadProgress<T> loadProgress;
        private SemaphoreSlim getActionSemaphore;

        //private object downloadLock;

        public TypeSerializationInfo SerializationInfo { get; }

        public Invoker<T, K?> IdInvoker { get; }

        public Invoker<T, int?> TypeInvoker { get; }

        public int TypeId { get; }

        public Type ItemType { get { return typeof(T); } }

        public SelectableList<T> Items { get; set; } = new SelectableList<T>();

        public bool IsSynchronized
        {
            get => Items.IsSynchronized;
            set
            {
                Items.IsSynchronized = value;
                Status = value ? ClientStatus.Compleate : ClientStatus.None;
            }
        }

        public virtual T DeserializeItem(JsonSerializer serializer, JsonTextReader jreader, T item, IList sourceList)
        {
            var property = (PropertySerializationInfo)null;
            var id = (object)null;
            var synchItem = item as ISynchronized;
            while (jreader.Read() && jreader.TokenType != JsonToken.EndObject)
            {
                if (jreader.TokenType == JsonToken.PropertyName)
                {
                    property = SerializationInfo.GetProperty((string)jreader.Value);
                }
                else
                {
                    var currentValue = item == null ? null : property?.Invoker.GetValue(item);
                    if (currentValue is IList listValue && synchItem != null && synchItem.SyncStatus == SynchronizedStatus.Load)
                    {
                        foreach (var listItem in listValue)
                        {
                            if (listItem is ISynchronized synchronized && synchronized.SyncStatus != SynchronizedStatus.Actual)
                            {
                                synchronized.SyncStatus = SynchronizedStatus.Load;
                            }
                        }
                    }
                    object value = DeserializeValue(serializer, jreader, property?.DataType, currentValue, null);
                    if (property == null)
                        continue;

                    if (property.Name == TypeInvoker?.Name && value != null)
                    {
                        var typeId = (int)value;
                        if (typeId != TypeId)
                        {
                            var client = Provider.GetClient(typeof(T), typeId);
                            return (T)client.DeserializeItem(serializer, jreader, item, (IList)sourceList);
                        }
                        continue;
                    }
                    if (property.Name == IdInvoker?.Name)
                    {
                        id = value;
                        if (item == null && id != null)
                        {
                            item = Select((K)id) ?? SelectBase(id);
                        }
                        if (item == null)
                        {
                            item = AddDownloads((K)id, (p) => NewLoadItem());
                            if (item == null)
                            {
                                item = NewLoadItem();
                                SetDownloads((K)id, item);
                            }
                        }
                        else if (!Items.Contains(item))
                        {
                            item = AddDownloads((K)id, item);
                        }
                        IdInvoker.SetValue(item, id);

                        if (item is ISynchronized synchronized)
                        {
                            synchItem = synchronized;
                            if (synchItem.SyncStatus == SynchronizedStatus.Actual)
                            {
                                synchItem.SyncStatus = SynchronizedStatus.Load;
                            }
                        }

                        continue;
                    }
                    if (item == null)
                    {
                        throw new Exception("Wrong Json properties sequence!");
                    }
                    if (synchItem != null && synchItem.SyncStatus != SynchronizedStatus.Load
                        && synchItem.Changes.ContainsKey(property.Name))
                    {
                        continue;
                    }
                    property.Invoker.SetValue(item, value);
                }
            }
            if (item == null)
                return null;

            if (synchItem != null && synchItem.SyncStatus == SynchronizedStatus.Load)
            {
                synchItem.SyncStatus = SynchronizedStatus.Actual;
            }
            if (RemoveDownloads((K)id))
            {
                Add(item);
            }
            return item;
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
                ? downloads.TryRemove(id, out var item)
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

        public T AddDownloads(K id, Func<K, T> item)
        {
            return downloads != null
                 ? downloads.GetOrAdd(id, item)
                 : GetBaseClient()?.AddDownloads(id, item) as T;
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

        private T SelectBase(object id)
        {
            return GetBaseClient()?.Select(id) as T;
        }

        public object DeserializeItem(JsonSerializer serializer, JsonTextReader jreader, object item, IList sourceList)
        {
            return DeserializeItem(serializer, jreader, item as T, sourceList);
        }

        public override R DeserializeObject<R>(JsonSerializer serializer, JsonTextReader jreader, R item, IList sourceList)
        {
            if (typeof(R) == typeof(T))
            {
                return (R)(object)DeserializeItem(serializer, jreader, (T)(object)item, sourceList);
            }
            return base.DeserializeObject(serializer, jreader, item, sourceList);
        }

        public ICRUDClient GetBaseClient()
        {
            if (TypeId == 0 || baseClient != null)
                return baseClient;
            var type = typeof(T).BaseType;
            var result = (ICRUDClient)null;
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

        public bool Add(object item)
        {
            return Add((T)item);
        }

        public virtual bool Add(T item)
        {
            var added = false;
            if (!Items.Contains(item))
            {
                Items.Add(item);
                added = true;
            }
            GetBaseClient()?.Add(item);
            return added;
        }

        public bool Remove(object item)
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
            return await GetAsync(IdInvoker.GetValue(item), ProgressToken.None).ConfigureAwait(false);
        }

        public async virtual Task<bool> Delete(T item)
        {
            var result = await DeleteAsync(IdInvoker.GetValue(item), ProgressToken.None).ConfigureAwait(false);
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

        private async Task GetAction(K id, Action<T> loadAction)
        {
            if (getActionSemaphore == null)
                getActionSemaphore = new SemaphoreSlim(2);
            await getActionSemaphore.WaitAsync();
            try
            {
                Debug.WriteLine($"Client.Get {typeof(T)} {id}");
                var result = await GetAsync(id, ProgressToken.None).ConfigureAwait(false);
                loadAction?.Invoke(result);
            }
            finally
            {
                getActionSemaphore.Release();
            }
        }

        public virtual Task<List<T>> GetAsync(ProgressToken progressToken)
        {
            IsSynchronized = true;
            return Task.FromResult<List<T>>(null);
        }

        public async Task<IEnumerable> GetAsync() => await GetAsync(ProgressToken.None);

        public string GetFilePath(IFileModel fileModel)
        {
            return GetFilePath(fileModel, $"/api/{typeof(T).Name}/DownloadFile/{{id}}");
        }

        public LoadProgress<T> Load(string filter, IProgressable progressable)
        {
            if (loadProgress == null || loadProgress.Task.IsCompleted || loadProgress.Filter != filter)
            {
                if (loadProgress != null && !loadProgress.Task.IsCompleted)
                {
                    loadProgress.Token.Cancel();
                }
                loadProgress = new LoadProgress<T>(filter, progressable);
                loadProgress.Task = string.IsNullOrEmpty(filter)
                    ? GetAsync(loadProgress.Token)
                    : FindAsync(filter, loadProgress.Token);
            }
            return loadProgress;
        }

        public virtual Task<List<T>> FindAsync(string filter, ProgressToken progressToken) => Task.FromResult<List<T>>(null);

        public async Task<IEnumerable> FindAsync(string filter) => await FindAsync(filter, ProgressToken.None);

        public virtual Task<T> GetAsync(K id, ProgressToken progressToken) => Task.FromResult(default(T));

        public Task<T> GetAsync(object id, ProgressToken progressToken) => GetAsync((K)id, progressToken);

        public async Task<object> GetAsync(object id) => await GetAsync((K)id, ProgressToken.None);

        public virtual Task<T> CopyAsync(K id, ProgressToken progressToken) => Task.FromResult(default(T));

        public Task<T> CopyAsync(object id, ProgressToken progressToken) => CopyAsync((K)id, progressToken);

        public async Task<object> CopyAsync(object id) => await CopyAsync((K)id, ProgressToken.None);

        public virtual Task<T> PutAsync(T value, ProgressToken progressToken) => Task.FromResult(value);

        public Task<T> PutAsync(object value, ProgressToken progressToken) => PutAsync((T)value, progressToken);

        public async Task<object> PutAsync(object value) => await PutAsync((T)value, ProgressToken.None);

        public virtual Task<T> PostAsync(T value, ProgressToken progressToken) => Task.FromResult(value);

        public Task<T> PostAsync(object value, ProgressToken progressToken) => PostAsync((T)value, progressToken);

        public async Task<object> PostAsync(object value) => await PostAsync((T)value, ProgressToken.None);

        public virtual Task<bool> DeleteAsync(K id, ProgressToken progressToken) => Task.FromResult(true);

        public Task<bool> DeleteAsync(object id, ProgressToken progressToken) => DeleteAsync((K)id, progressToken);

        public Task<bool> DeleteAsync(object id) => DeleteAsync((K)id, ProgressToken.None);

        public virtual Task<object> GenerateIdAsync(ProgressToken progressToken) => Task.FromResult<object>(null);

        public Task<object> GenerateId() => GenerateIdAsync(ProgressToken.None);

        public virtual Task<T> MergeAsync(K id, List<string> ids, ProgressToken progressToken) => Task.FromResult<T>(null);

        public Task<T> MergeAsync(T item, List<string> ids, ProgressToken progressToken)
            => MergeAsync(IdInvoker.GetValue(item).Value, ids, ProgressToken.None);

        public async Task<object> MergeAsync(object id, List<string> ids) => await MergeAsync((K)id, ids, ProgressToken.None);
    }

    public class LoadProgress<T>
    {
        public LoadProgress(string filter, IProgressable progressable)
        {
            Filter = filter;
            Token = new ProgressToken(progressable);
        }
        public ProgressToken Token { get; }
        public Task<List<T>> Task { get; set; }
        public string Filter { get; internal set; }
    }
}