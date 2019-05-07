using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public abstract partial class Client<T, K> : ClientBase, ICRUDClient<T> where T : class, new() where K : struct
    {
        public Client(Invoker<T, K?> idInvoker, Invoker<T, int?> typeInvoker, int typeId = 0)
        {
            IdInvoker = idInvoker;
            Items.Indexes.Add(IdInvoker);
            TypeInvoker = typeInvoker;
            TypeId = typeId;
            SerializationInfo = new TypeSerializationInfo(typeof(T));
        }
        private Dictionary<K, T> downloadItems = new Dictionary<K, T>();

        public TypeSerializationInfo SerializationInfo;
        private ICRUDClient baseClient;
        private object lockObject = new object();
        private LoadProgress<T> loadProgress;

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
                            item = Select((K)id) ?? (T)GetBaseClient()?.Select(id);
                            //(T)sourceList?
                            //.Cast<IPrimaryKey>()
                            //.FirstOrDefault(p => p?.PrimaryKey?.Equals(id) ?? false);
                            if (item is ISynchronized synchronized)
                            {
                                synchItem = synchronized;
                                if (synchItem.SyncStatus == SynchronizedStatus.Actual)
                                    synchItem.SyncStatus = SynchronizedStatus.Load;
                            }
                        }
                        if (item == null)
                        {
                            item = new T();
                            if (item is ISynchronized synchronized)
                            {
                                synchItem = synchronized;
                                synchItem.SyncStatus = SynchronizedStatus.Load;
                            }
                        }

                        IdInvoker.SetValue(item, id);
                        if (!Items.Contains(item))
                        {
                            downloadItems[(K)id] = item;
                        }
                        GetBaseClient()?.Add(item);

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
            downloadItems.Remove((K)id);
            Add(item);
            return item;
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
            var removed = Items.Remove((T)item);
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
                        p.SyncStatus == SynchronizedStatus.New
                        || p.SyncStatus == SynchronizedStatus.Edit);
        }

        public object Select(object id)
        {
            if (id == null)
                return null;
            return Select((K)id);
        }

        public virtual T Select(K id)
        {
            var item = Items.SelectOne(IdInvoker.Name, (K?)id);
            if (item == null)
            {
                downloadItems.TryGetValue(id, out item);
            }
            return item;
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
            lock (lockObject)
            {
                var item = Select(id);
                if (item == null)
                {
                    item = new T();
                    IdInvoker.SetValue(item, id);
                    downloadItems[id] = item;
                    _ = GetAsync(id);
                }
                return item;
            }
        }

        public virtual Task<List<T>> GetAsync(ProgressToken progressToken)
        {
            IsSynchronized = true;
            return Task.FromResult<List<T>>(null);
        }

        public async Task<IEnumerable> GetAsync() => await GetAsync(ProgressToken.None);

        public LoadProgress<T> Load(string filter, IProgressable progressable)
        {
            if (loadProgress == null || loadProgress.Filter != filter)
            {
                if (loadProgress != null && !loadProgress.Task.IsCompleted)
                {
                    loadProgress.Token.Cancel();
                }
                loadProgress = new LoadProgress<T>(filter, progressable);
                loadProgress.Task = FindAsync(filter, loadProgress.Token);
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