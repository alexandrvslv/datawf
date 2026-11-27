using DataWF.Collections;
using DataWF.Invokers;
using DataWF.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF;

public abstract partial class WebTable<T, K> : WebClient, IWebTable<T>
    where T : class, new()
    where K : struct
{
    private IWebTable baseClient;
    private SemaphoreSlim getActionSemaphore;
    private TableLoads<T, K> loads;
    public WebTable(IInvokerGetter<T, K> idInvoker, IInvokerGetter<T, int> typeInvoker, int typeId = 0)
    {
        Name = typeof(T).Name;
        IdInvoker = idInvoker;
        Items = new TableItemList<T>(this) { AsyncNotification = true };
        Items.Indexes.Concurrent = true;
        Items.Indexes.AddIndex(IdInvoker);
        Items.CollectionChanged += OnItemsCollectionChanged;
        TypeInvoker = typeInvoker;
        TypeId = typeId;
        Converter = new SystemJsonClientConverter<T, K>(this);
    }

    public TableLoads<T, K> Loads => loads ??= Provider.GetLoads(this);

    public ITypeSerializartionInfo<T> Converter { get; }

    public IInvokerGetter<T, K> IdInvoker { get; }

    public IInvokerGetter<T, int> TypeInvoker { get; }

    public IInvoker PrimaryKey => IdInvoker;

    public IInvoker ItemTypeKey => TypeInvoker;

    public int TypeId { get; }

    public Type ItemType => typeof(T);

    public TableItemList<T> Items { get; set; }

    public bool IsSynchronized
    {
        get => Items.IsSynchronized;
        set
        {
            Items.IsSynchronized = value;
            Status = value ? WebClientStatus.Compleate : WebClientStatus.None;
        }
    }

    IModelSchema IModelItem.Schema => Schema;

    public IEnumerable<IInvoker> Columns => Converter.SerializationInfo.Properties.Select(x => x.Invoker);

    public IInvoker GetColumn(string name) => Converter.SerializationInfo.GetProperty(name)?.Invoker;

    public override void ClearCache()
    {
        IsSynchronized = false;
        Items.Clear();
        loads.ClearQueue();
        base.ClearCache();
    }

    public virtual bool ClearLoadCache(string filter)
    {
        return loads.ClearLoadCache(filter);
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

    internal T SelectBase(object id)
    {
        return GetBaseClient()?.Select(id) as T;
    }

    public IWebTable GetBaseClient()
    {
        if (TypeId == 0 || baseClient != null)
            return baseClient;
        var result = Schema.GetTable(typeof(T).BaseType);
        while (result != null && result.TypeId != 0 && result.ItemType.BaseType != null)
        {
            result = Schema.GetTable(result.ItemType.BaseType);
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
            removed = loads.RemoveDownloads((K)IdInvoker.GetValue(item));
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
                var client = Schema.GetTable(item.GetType());
                client?.Remove(item);
                return;
            }
            Remove(item);
        }
    }

    public bool Contains(object? item) => Items.Contains(item);

    public bool Contains(T item) => Items.Contains(item);

    IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => Items.GetEnumerator();

    public OneListEnumerator<T> GetEnumerator() => Items.GetEnumerator();

    public object ParseId(object id)
    {
        return Helper.Parse(id, IdInvoker.DataType);
    }

    IEnumerable IWebTable.GetChanges()
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
        return Items.SelectOne<K>(IdInvoker.Name, id);
    }

    public virtual T Select(K id)
    {
        return Items.SelectOne<K>(IdInvoker.Name, id) ?? GetDownloads(id);
    }

    public virtual T Get(object id)
    {
        return Get((K)id);
    }

    public virtual T Get(K id)
    {
        return Get(id, (Action<T>)null);
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
            var result = await Get(id, ProgressToken.None).ConfigureAwait(false);
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

    public Task<List<T>> Load(ProgressToken token)
    {
        return Load(string.Empty, token);
    }

    public async Task<List<T>> Load(string filter, ProgressToken token)
    {
        return await LoadCache(filter, token).Task.ConfigureAwait(false);
    }

    public LoadProgress<T> LoadCache(string filter, ProgressToken token)
    {
        filter = filter ?? string.Empty;
        if (!loadQueue.TryGetValue(filter, out var loadTask)
            || loadTask.Task.Status == TaskStatus.Faulted
            || loadTask.Task.Status == TaskStatus.Canceled)
        {
            loadTask = new LoadProgress<T>(filter, token);
            if (token.Pages == null)
            {
                loadQueue[filter] = loadTask;
            }
            loadTask.Task = string.IsNullOrEmpty(filter)
                ? Get(loadTask.Token)
                : Search(filter, loadTask.Token);
        }

        return loadTask;
    }

    public IEnumerable<object> SearchByValue(IInvoker valueInvoker, object value)
    {
        var index = Items.Indexes.AddIndex(valueInvoker);
        //if (result == null)
        //{
        //    var parameter = new QueryParameter<T> { Invoker = valueInvoker, Value = value };
        //    _ = LoadAsync(parameter.Format(false), null, HttpJsonSettings.Default, ProgressToken.None);
        //}
        return index.Scan(CompareOperator.Equal, value).Cast<object>();
    }

    public async virtual Task<bool> Delete(T item, ProgressToken token)
    {
        var result = await Delete(IdInvoker.GetValue(item), token).ConfigureAwait(false);
        if (result)
        {
            Remove(item);
        }
        return result;
    }

    public async virtual Task<T> Reload(T item, ProgressToken token)
    {
        if (item is ISynchronized synched)
            synched.SyncStatus = SynchronizedStatus.Load;
        return await Get(IdInvoker.GetValue(item), token).ConfigureAwait(false);
    }

    public virtual Task<List<T>> Get(ProgressToken token) => Task.FromResult<List<T>>(null);

    public virtual Task<List<T>> Search(string filter, ProgressToken token) => Task.FromResult<List<T>>(null);

    public virtual Task<T> Get(K id, ProgressToken token) => Task.FromResult(default(T));

    public Task<T> Get(object id, ProgressToken token) => Get((K)id, token);

    public virtual Task<T> Copy(K id, ProgressToken token) => Task.FromResult(default(T));

    public Task<T> Copy(object id, ProgressToken token) => Copy((K)id, token);

    public virtual Task<T> Put(T value, ProgressToken token) => Task.FromResult(value);

    public Task<T> Put(object value, ProgressToken token) => Put((T)value, token);

    public virtual Task<T> Post(T value, ProgressToken token) => Task.FromResult(value);

    public Task<T> PostAsync(object value, ProgressToken token) => Post((T)value, token);

    public virtual Task<List<T>> PostPackage(List<T> value, ProgressToken token) => Task.FromResult(value);

    public virtual Task<bool> Delete(K id, ProgressToken token) => Task.FromResult(true);

    public Task<bool> Delete(object id, ProgressToken token) => id is K key ? Delete(key, token) : Delete((T)id, token);

    public virtual Task<object> GenerateId(ProgressToken token) => Task.FromResult<object>(null);

    public virtual Task<T> Merge(K id, List<string> ids, ProgressToken token) => Task.FromResult<T>(null);

    public Task<T> Merge(T item, List<string> ids, ProgressToken token) => Merge(IdInvoker.GetValue(item), ids, token);

    async Task<object> IWebTable.Reload(object item, ProgressToken token) => await Reload((T)item, token);
    async Task<IEnumerable> IWebTable.Load(string filter, ProgressToken token) => await Load(filter, token);
    ILoadProgress IWebTable.LoadCache(string filter, ProgressToken token) => LoadCache(filter, token);
    async Task<IEnumerable> IWebTable.Get(ProgressToken token) => await Get(token);
    async Task<IEnumerable> IWebTable.Search(string filter, ProgressToken token) => await Search(filter, token);
    async Task<object> IWebTable.Get(object id, ProgressToken token) => await Get((K)id, token);
    async Task<object> IWebTable.Copy(object id, ProgressToken token) => await Copy((K)id, token);
    async Task<object> IWebTable.Put(object value, ProgressToken token) => await Put((T)value, token);
    async Task<object> IWebTable.Post(object value, ProgressToken token) => await Post((T)value, token);
    async Task<IList> IWebTable.PostPackage(IList value, ProgressToken token) => await PostPackage((List<T>)value, token);
    async Task<object> IWebTable.Merge(object id, List<string> ids, ProgressToken token) => await Merge((K)id, ids, token);


}