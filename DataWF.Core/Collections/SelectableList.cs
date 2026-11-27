using DataWF.Collections.Indexes;
using DataWF.Collections.Query;
using DataWF.Comparers;
using DataWF.Invokers;
using DataWF.Serialization;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Collections;

[Invoker]
public partial class SelectableList<T> : ISelectable<T>, IList, IList<T>
{
    protected readonly IndexList<T> indexes;
    protected ArrayPointerList<T> items;
    protected IComparer<T>? comparer;
    protected PropertyChangedEventHandler? propertyHandler;
    protected SelectableListView<T>? defaultView;
    private bool isSynchronized;
    protected object lockObject = new object();
    //Async notify
    private int notifySemafore = 0;
    private ConcurrentQueue<Tuple<object, EventArgs>> notifyQueue;
    private bool asyncNotification;

    public SelectableList(int capacity)
    {
        items = new ArrayPointerList<T>(capacity);
        indexes = new IndexList<T>(items);
        if (typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(T)))
        {
            propertyHandler = OnItemPropertyChanged;
        }
    }

    public SelectableList() : this(0)
    { }

    public SelectableList(IEnumerable<T> items, IComparer<T>? comparer = null)
    : this(items.Count())
    {
        this.comparer = comparer;
        AddRangeInternal(items, false);
    }


    [ElementSerializer(Ignore = true), Browsable(false)]
    public IndexList<T> Indexes => indexes;


    [ElementSerializer(Ignore = true), Browsable(false)]
    public int Capacity
    {
        get => items.Capacity;
        set => items.Capacity = value;
    }

    [ElementSerializer(Ignore = true), Browsable(false)]
    public bool IsFixedSize => false;

    [ElementSerializer(Ignore = true), Browsable(false)]
    public Type ItemType => typeof(T);

    [ElementSerializer(Ignore = true), Browsable(false)]
    public bool IsSorted => comparer != null;

    [ElementSerializer(Ignore = true)]
    public IComparer<T>? Comparer => comparer;

    IFilterable<T> ISelectable<T>.DefaultView => DefaultView;

    [ElementSerializer(Ignore = true), Browsable(false)]
    public SelectableListView<T> DefaultView => defaultView ??= new SelectableListView<T>(this);

    public virtual int Count => items.Count;

    [ElementSerializer(Ignore = true), Browsable(false)]
    public bool IsReadOnly => false;

    [ElementSerializer(Ignore = true), Browsable(false)]
    public virtual bool IsSynchronized
    {
        get => isSynchronized;
        set => isSynchronized = value;
    }

    [ElementSerializer(Ignore = true), Browsable(false)]
    public object SyncRoot => items;

    [ElementSerializer(Ignore = true)]
    public bool CheckUnique { get; set; } = true;

    [ElementSerializer(Ignore = true)]
    public bool Disposed => items == null;

    [ElementSerializer(Ignore = true), Browsable(false)]
    public bool AsyncNotification
    {
        get => asyncNotification;
        set
        {
            asyncNotification = value;
            if (asyncNotification)
            {
                notifyQueue = new ConcurrentQueue<Tuple<object, EventArgs>>();
            }
        }
    }

    [ElementSerializer(Ignore = true), Browsable(false)]
    public bool IsHandled => PropertyChanged != null;

    public IEnumerable<TT> GetHandlers<TT>() => EventDelegateExtensions.GetContainers<TT, NotifyCollectionChangedEventHandler>(CollectionChanged);

    [ElementSerializer(Ignore = true), Browsable(false)]
    public IEnumerable<INotifyListPropertyChanged> Containers => EventDelegateExtensions.GetContainers<INotifyListPropertyChanged, PropertyChangedEventHandler>(PropertyChanged);

    [ElementSerializer(Ignore = true), Browsable(false)]
    public IEnumerable<IFilterable> Views => EventDelegateExtensions.GetContainers<IFilterable, NotifyCollectionChangedEventHandler>(CollectionChanged);

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangedEventHandler? ItemPropertyChanged;


    #region Use Index
    IQQuery Query.IQueryProvider.Query()
        => Query();

    public IQQuery<T> Query()
        => new QQuery<T>(this);

    public IEnumerable<T> Select(IQParameter parameter)
        => parameter.Filter(this);

    public IEnumerable<T> Select(IQueryInvoker invoker, CompareOperator comparer, object value)
        => Select(invoker.CreateQParameter(comparer, value, this));

    public IEnumerable<T> Select(string property, CompareOperator comparer, object value)
        => Select((IQueryInvoker)InvokeProvider.GetInvoker<T>(property), comparer, value);

    public IEnumerable<T> Select<K>(IQueryInvoker<T, K> invoker, CompareOperator comparer, object value)
    {
        if (indexes.GetIndex(invoker.Name) is IIndex<T, K> index)
        {
            return index.Select(comparer, value);
        }
        return Search<K>(invoker, comparer, value);
    }

    public IEnumerable<T> Select<K>(IQueryInvoker<T, K> invoker, CompareOperator comparer, K value)
    {
        if (indexes.GetIndex(invoker.Name) is IIndex<T, K> index)
        {
            return index.Select(comparer, value);
        }
        return Search<K>(invoker, comparer, value);
    }

    private IEnumerable<T> Search<K>(IQueryInvoker<T, K> invoker, CompareOperator comparer, object value)
    {
        var parameter = new QCompareParameter(invoker, comparer, value);
        return parameter.Filter(this);
    }

    private IEnumerable<T> Search<K>(IQueryInvoker<T, K> invoker, CompareOperator comparer, K value)
    {
        var parameter = new QCompareParameter(invoker, comparer, value);
        return parameter.Filter(this);
    }

    private IEnumerable<T> Search<K>(string propertyName, CompareOperator comparer, object value) =>
        Search((IQueryInvoker<T, K>)InvokeProvider.GetInvoker<T>(propertyName), comparer, value);

    private IEnumerable<T> Search<K>(string propertyName, CompareOperator comparer, K value) =>
        Search((IQueryInvoker<T, K>)InvokeProvider.GetInvoker<T>(propertyName), comparer, value);

    public T? SelectOne<K>(string property, K value)
    {
        if (indexes.GetIndex(property) is IIndex<T, K> index)
        {
            return index.SelectOne(value);
        }
        return SearchOne<K>(property, CompareOperator.Equal, value);
    }

    public T? SelectOne(string property, object value) => SelectOne(property, CompareOperator.Equal, value);

    public T? SelectOne(string property, CompareOperator comparer, object value)
    {
        var index = indexes.GetIndex(property);
        if (index != null)
        {
            return index.SelectOne(value);
        }
        return SearchOne(property, comparer, value);
    }

    public T? SelectOne<K>(IQueryInvoker<T, K> property, CompareOperator comparer, K value)
    {
        var index = indexes.GetIndex(property.Name);
        if (index is IIndex<T, K> typedIndex)
        {
            return typedIndex.SelectOne(value);
        }
        return SearchOne(property, comparer, value);
    }

    #endregion

    public virtual T NewItem() => InvokeProvider.CreateObject<T>()!;

    object ISortable.NewItem() => NewItem()!;

    public virtual void Dispose()
    {
        //_Properties.Clear();
        comparer = null;
        propertyHandler = null;
        items?.Clear();
        items = null!;
    }

    private void EnqueueNotification(object sender, EventArgs e)
    {
        notifyQueue.Enqueue(new Tuple<object, EventArgs>(sender, e));

        if (Interlocked.CompareExchange(ref notifySemafore, 1, 0) == 0)
        {
            Task.Run(DequeueNotification);
        }
    }

    private async void DequeueNotification()
    {
        try
        {
            await Task.Delay(20);
            while (notifyQueue.TryDequeue(out var args))
            {
                if (args.Item2 is NotifyCollectionChangedEventArgs collectionArgs
                    && collectionArgs.Action == NotifyCollectionChangedAction.Add)
                {
                    args = MergeCollectionAdd(args, collectionArgs);
                }
                if (args.Item2 is PropertyChangedEventArgs propertyArgs)
                {
                    args = MergeProperty(args, propertyArgs);
                }
                ProcessNotification(args.Item1, args.Item2);
            }
        }
        finally
        {
            Interlocked.Decrement(ref notifySemafore);
        }
    }

    private Tuple<object, EventArgs> MergeProperty(Tuple<object, EventArgs> args, PropertyChangedEventArgs propertyArgs)
    {
        PropertyChangedAggregateEventArgs newArgs = null;
        while (notifyQueue.TryPeek(out var next)
            && next.Item2 is PropertyChangedEventArgs nextArgs
            && next.Item1.Equals(args.Item1)
            && notifyQueue.TryDequeue(out next))
        {
            if (newArgs == null)
            {
                newArgs = new PropertyChangedAggregateEventArgs(propertyArgs);
            }
            newArgs.Items.Add(nextArgs);
        }
        return newArgs == null ? args : new Tuple<object, EventArgs>(args.Item1, newArgs);
    }

    private Tuple<object, EventArgs> MergeCollectionAdd(Tuple<object, EventArgs> args, NotifyCollectionChangedEventArgs collectionArgs)
    {
        var list = new List<T>(4);
        foreach (T item in collectionArgs.NewItems)
        {
            list.Add(item);
        }
        while (notifyQueue.TryPeek(out var next)
            && next.Item2 is NotifyCollectionChangedEventArgs nextArgs
            && nextArgs.Action == NotifyCollectionChangedAction.Add
            && notifyQueue.TryDequeue(out next))
        {
            foreach (T nextItem in nextArgs.NewItems)
            {
                list.Add(nextItem);
            }
        }
        if (list.Count > 1)
        {
            args = new Tuple<object, EventArgs>(args.Item1,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, list, collectionArgs.NewStartingIndex));
            //if (list.Count > 100)
            //{
            //    System.Diagnostics.Debug.WriteLine($"Join Add Notifications: {list.Count}");
            //}
        }

        return args;
    }

    private void ProcessNotification(object sender, EventArgs e)
    {
        if (e is NotifyCollectionChangedEventArgs collectionArgs)
        {
            CollectionChanged?.Invoke(this, collectionArgs);
        }
        else if (e is PropertyChangedEventArgs propertyArgs)
        {
            if (sender == this)
            {
                PropertyChanged?.Invoke(sender, propertyArgs);
            }
            else
            {
                ItemPropertyChanged?.Invoke(sender, propertyArgs);
            }
        }
    }

    public virtual NotifyCollectionChangedEventArgs OnCollectionChanged(NotifyCollectionChangedAction type, object? item = null, int index = -1, int oldIndex = -1, object? oldItem = null)
    {
        var args = (NotifyCollectionChangedEventArgs?)null;
        if (CollectionChanged != null)
        {
            args = ListHelper.GenerateArgs(type, item, index, oldIndex, oldItem);
            if (AsyncNotification)
            {
                EnqueueNotification(this, args);
            }
            else
            {
                CollectionChanged(this, args);
            }
        }
        OnPropertyChanged(nameof(SyncRoot));
        return args;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string property = "")
    {
        if (PropertyChanged != null)
        {
            var args = new PropertyChangedEventArgs(property);
            if (AsyncNotification)
            {
                EnqueueNotification(this, args);
            }
            else
            {
                PropertyChanged(this, args);
            }
        }
    }

    public virtual void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        OnItemPropertyChanged((T)sender, -1, e);
    }

    public void OnItemPropertyChanged(T item, int index, PropertyChangedEventArgs e)
    {
        if (indexes.Count > 0)
        {
            if (e is PropertyChangedAggregateEventArgs aggregator)
            {
                foreach (var entry in aggregator.Items)
                {
                    CheckIndex(item, entry);
                }
            }
            else
            {
                CheckIndex(item, e);
            }
        }
        if (IsSorted)
        {
            Move(item, index);
        }
        if (ItemPropertyChanged != null)
        {
            if (AsyncNotification)
            {
                EnqueueNotification(item, e);
            }
            else
            {
                ItemPropertyChanged(item, e);
            }
        }
    }

    protected virtual void Move(T item, int index)
    {
        if (index < 0)
            index = IndexOf(item);
        int newindex = GetIndexBySort(item);
        if (newindex < 0)
            newindex = -newindex - 1;
        if (index != newindex)
        {
            if (newindex > index)
                newindex--;
            if (newindex > items.Count)
                newindex = items.Count;
            items.RemoveAt(index);
            items.Insert(newindex, item);
            OnCollectionChanged(NotifyCollectionChangedAction.Move, item, newindex, index);
        }
    }

    private void CheckIndex(T item, PropertyChangedEventArgs e)
    {
        indexes.GetIndex(e.PropertyName)?.RefreshItem(item, e);
    }

    public bool IsFirst(T item) => items.Count > 0 && items[0].Equals(item);

    public bool IsLast(T item) => items.Count > 0 && items[items.Count - 1].Equals(item);

    public void ClearInternal()
    {
        lock (lockObject)
        {
            if (propertyHandler != null)
            {
                foreach (var item in this)
                {
                    if (item is INotifyPropertyChanged notify)
                    {
                        notify.PropertyChanged -= propertyHandler;
                    }
                }
            }
            indexes.Clear();
            items.Clear();
        }
    }

    public virtual void Clear()
    {
        if (items.Count > 0)
        {
            ClearInternal();
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }
    }

    public void Reset() => OnCollectionChanged(NotifyCollectionChangedAction.Reset);

    public virtual void InsertInternal(int index, T item)
    {
        if (item == null)
            return;

        indexes.AddToIndex(item);
        items.Insert(index, item);

        if (propertyHandler != null)
        {
            ((INotifyPropertyChanged)item).PropertyChanged += propertyHandler;
        }
    }

    void IList.Insert(int index, object? item) => Insert(index, (T)item);

    public virtual void Insert(int index, T item)
    {
        InsertInternal(index, item);
        OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
    }

    public virtual int AddInternal(T item)
    {
        lock (lockObject)
        {
            int index = GetIndexForAdding(item);
            if (index < 0)
            {
                index = ~index;
                if (index > items.Count)
                {
                    index = items.Count;
                }

                InsertInternal(index, item);
            }
            else
            {
                index = -1;
            }
            return index;
        }
    }

    protected int GetIndexForAdding(T item) => GetIndexForAdding(item, CheckUnique);

    protected int GetIndexForAdding(T item, bool checkUnique)
    {
        if (comparer != null)
        {
            var index = ListHelper.BinarySearch(items, item, comparer, true);
            return checkUnique ? index : -Math.Abs(index);
        }
        return checkUnique
            ? Contains(item) ? items.Count : -(items.Count + 1)
            : -(items.Count + 1);
    }

    protected int GetIndexBySort(T item)
    {
        if (comparer != null)
        {
            return ListHelper.BinarySearch(items, item, comparer, true);
        }
        return Contains(item) ? IndexOf(item) : -(items.Count + 1);
    }

    int IList.Add(object? item) => Add((T)item);

    void ICollection<T>.Add(T item) => Add(item);

    public virtual int Add(T item)
    {
        int index = AddInternal(item);
        if (index >= 0)
        {
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        }
        return index;
    }

    public virtual void RemoveInternal(T item, int index)
    {
        if (index < 0)
            return;
        if (propertyHandler != null && item is INotifyPropertyChanged notified)
        {
            notified.PropertyChanged -= propertyHandler;
        }
        if (item != null)
        {
            indexes.RemoveFromIndex(item);
        }
        items.RemoveAt(index);
    }

    public void Remove(T item, int index)
    {
        RemoveInternal(item, index);
        OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
    }

    public void Remove(object? item) => Remove((T)item);

    public virtual bool Remove(T item)
    {
        lock (lockObject)
        {
            int index = IndexOf(item);
            if (index == -1)
                return false;
            Remove(item, index);
            return true;
        }
    }

    public void RemoveAt(int index) => Remove(items[index], index);

    public virtual void AddRangeInternal(IEnumerable<T> list, bool checkUnique)
    {
        lock (lockObject)
        {
            int index = 0;
            foreach (T item in list)
            {
                index = GetIndexForAdding(item, checkUnique);
                if (index < 0)
                {
                    index = -index - 1;
                    if (index > items.Count)
                    {
                        index = items.Count;
                    }

                    InsertInternal(index, item);
                }
            }
        }
    }

    public void AddRange(IEnumerable items) => AddRange(items.ToEnumerable<T>());

    public void AddRange(IEnumerable<T> items) => AddRange(items, CheckUnique);

    public void AddRange(IEnumerable<T> items, bool checkUnique)
    {
        AddRangeInternal(items, checkUnique);
        OnCollectionChanged(NotifyCollectionChangedAction.Add, items.ToList());
    }

    private void RemoveRangeInternal(IEnumerable<T> items)
    {
        lock (lockObject)
        {
            int index = 0;
            foreach (T item in items)
            {
                index = IndexOf(item);
                if (index == -1)
                {
                    continue;
                }

                RemoveInternal(item, index);
            }
        }
    }

    public void RemoveRange(IEnumerable items) => RemoveRange(items.ToEnumerable<T>());

    public void RemoveRange(IEnumerable<T> items)
    {
        RemoveRangeInternal(items);
        OnCollectionChanged(NotifyCollectionChangedAction.Remove, items is IList iList ? iList : items.ToList());
    }

    public virtual int IndexOf(object? item) => IndexOf((T)item);

    public virtual int IndexOf(T item)
    {
        if (comparer != null)
        {
            var index = ListHelper.BinarySearch(items, item, comparer);

            if (index >= 0 && index < items.Count && item.Equals(items[index]))
            {
                return index;
            }
        }
        return items.IndexOf(item);
    }

    public T this[int index]
    {
        get => GetItemInternal(index);
        set
        {
            T item = index < Count ? items[index] : default(T);

            if (EqualityComparer<T>.Default.Equals(item, value))
                return;
            var valueIndex = IndexOf(value);
            if (valueIndex >= 0)
            {
                items.RemoveAt(valueIndex);
                items.Insert(index, value);
                OnCollectionChanged(NotifyCollectionChangedAction.Move, value, index, valueIndex, item);
            }
            else if (item != null)
            {
                RemoveInternal(item, index);
                InsertInternal(index, value);
                OnCollectionChanged(NotifyCollectionChangedAction.Replace, value, index, index, item);
            }
            else
            {
                InsertInternal(index, value);
                OnCollectionChanged(NotifyCollectionChangedAction.Add, value, index);
            }
        }
    }

    object IList.this[int index]
    {
        get => GetItem(index);
        set => this[index] = (T)value;
    }

    public object GetItem(int index) => GetItemInternal(index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual T GetItemInternal(int index) => items[index];

    public void ApplySortInternal(params string[] property)
    {
        var comparerList = new InvokerComparerList<T>();
        foreach (string column in property)
        {
            var direction = ListSortDirection.Ascending;
            if (column.EndsWith(" DESC", StringComparison.OrdinalIgnoreCase))
                direction = ListSortDirection.Descending;
            var index = column.IndexOf(" ", StringComparison.Ordinal);
            var invoker = (IQueryInvoker)InvokeProvider.GetInvoker<T>(index > 0 ? column.Substring(0, index) : column);
            comparerList.Add((IComparer<T>)invoker.CreateComparer(direction));
        }
        ApplySortInternal(comparerList);
    }

    public void ApplySortInternal(string property, ListSortDirection direction)
    {
        var invoker = InvokeProvider.GetInvoker<T>(property);
        ApplySortInternal(invoker.CreateComparer<T>(direction));
    }

    public virtual void ApplySortInternal(IComparer<T>? comparer)
    {
        lock (lockObject)
        {
            this.comparer = comparer;
            UpdateSort();
        }
    }

    public void UpdateSort() => ListHelper.QuickSort<T>(items, comparer);//_items.Sort(comparer);

    void ISortable.ApplySort(IComparer? comparer) => ApplySort(comparer == null ? null
        : comparer is IComparer<T> gcomparer ? gcomparer
        : throw new Exception("Wrong Comparer Type"));

    public virtual void ApplySort(IComparer<T>? comparer)
    {
        if (this.comparer != null && this.comparer.Equals(comparer))
            return;
        ApplySortInternal(comparer);
        OnCollectionChanged(NotifyCollectionChangedAction.Reset);
    }

    public T? Find(QQuery<T> query) => Select(query).FirstOrDefault();

    public T? SearchOne(PropertyInfo property, object value) => SearchOne(property.Name, CompareOperator.Equal, value);

    public T? SearchOne(string property, CompareOperator comparer, object value) => Search(property, comparer, value).FirstOrDefault();

    public T? SearchOne<K>(string property, CompareOperator comparer, object value) => Search(property, comparer, value).FirstOrDefault();

    public T? SearchOne<K>(IQueryInvoker<T, K> property, CompareOperator comparer, K value) => Search(property, comparer, value).FirstOrDefault();

    public void RemoveSort()
    {
        comparer = null;
        Sort();
    }

    public void SortInternal() => ListHelper.QuickSort<T>(items, comparer);

    public void Sort()
    {
        SortInternal();
        OnCollectionChanged(NotifyCollectionChangedAction.Reset);
    }

    public void Sort(Comparison<T> comp) => items.Sort(comp);

    public void Sort(IComparer<T> comp) => ListHelper.QuickSort<T>(items, comp);

    public object? GetPrev(object item)
    {
        int index = items.IndexOf((T)item);
        if (index <= 0)
            return null;
        return items[--index];
    }

    public object? GetNext(object item)
    {
        int index = items.IndexOf((T)item);
        if (index < 0 || index >= Count - 1)
            return null;
        return items[++index];
    }

    public T First() => items.Count == 0 ? throw new IndexOutOfRangeException() : items[0];

    public T? FirstOrDefault() => items.Count == 0 ? default : items[0];

    public T Last() => items.Count == 0 ? throw new IndexOutOfRangeException() : items[items.Count - 1];

    public T? LastOrDefault() => items.Count == 0 ? default : items[items.Count - 1];

    #region ICollection Members
    public bool Contains(object? item) => Contains((T)item);

    public virtual bool Contains(T item)
    {
        if (item is IEntryNotifyPropertyChanged containered && propertyHandler != null)
        {
            return containered.Containers.Contains(this);
        }
        return items.Contains(item);
    }

    public void CopyTo(T[] array, int index) => items.CopyTo(array, index);

    void ICollection.CopyTo(Array array, int index) => ArrayList.Adapter(items).CopyTo(array, index);

    #endregion

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    public OneArrayPointerEnumerator<T> GetEnumerator() => items.GetEnumerator();
}
