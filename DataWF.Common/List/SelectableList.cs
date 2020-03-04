using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public class SelectableList<T> : ISelectable, ISelectable<T>, IList, IList<T>
    {
        protected ListIndexes<T> indexes = new ListIndexes<T>();
        protected Type type;
        protected List<T> items;
        protected IComparer<T> comparer;
        protected InvokerComparer<T> searchComparer;
        protected PropertyChangedEventHandler propertyHandler;
        protected SelectableListView<T> defaultView;
        private bool isSynchronized;
        protected object lockObject = new object();

        public SelectableList(int capacity)
        {
            items = new List<T>(capacity);
            type = typeof(T);
            if (TypeHelper.IsInterface(type, typeof(INotifyPropertyChanged)))
            {
                propertyHandler = OnItemPropertyChanged;
            }
        }

        public SelectableList() : this(0)
        { }

        public SelectableList(IEnumerable<T> items, IComparer<T> comparer = null) : this(items.Count())
        {
            this.comparer = comparer;
            AddRangeInternal(items);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangedEventHandler ItemPropertyChanged;

        [XmlIgnore, Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, Browsable(false)]
        public IEnumerable<INotifyListPropertyChanged> Containers => TypeHelper.GetContainers(PropertyChanged);

        [XmlIgnore, Browsable(false)]
        public ListIndexes<T> Indexes
        {
            get { return indexes; }
        }

        [XmlIgnore, Browsable(false)]
        public int Capacity
        {
            get { return items.Capacity; }
            set { items.Capacity = value; }
        }

        [Browsable(false)]
        public bool IsFixedSize
        {
            get { return false; }
        }

        [XmlIgnore]
        [Browsable(false)]
        public Type ItemType
        {
            get { return type; }
            set { type = value; }
        }

        [Browsable(false)]
        public bool IsSorted
        {
            get { return comparer != null; }
        }

        public IComparer<T> Comparer => comparer;

        IComparer ISortable.Comparer => comparer as IComparer;

        IFilterable ISortable.DefaultView
        {
            get { return DefaultView; }
        }

        [XmlIgnore, Browsable(false)]
        public SelectableListView<T> DefaultView
        {
            get
            {
                if (defaultView == null)
                    defaultView = new SelectableListView<T>(this);
                return defaultView;
            }
        }

        public int Count
        {
            get { return items.Count; }
        }

        [Browsable(false)]
        public bool IsReadOnly
        {
            get { return false; }
        }

        [Browsable(false), XmlIgnore]
        public virtual bool IsSynchronized
        {
            get { return isSynchronized; }
            set { isSynchronized = value; }
        }

        [Browsable(false)]
        public object SyncRoot
        {
            get { return items; }
        }

        #region Use Index
        IEnumerable ISelectable.Select(IQuery query)
        {
            return query is Query<T> typed
                ? Select(typed)
                : ListHelper.Select(this, query, indexes);
        }

        public IEnumerable<T> Select(Query<T> query)
        {
            return ListHelper.Select<T>(this, query, indexes);
        }

        IEnumerable ISelectable.Select(IQueryParameter parameter)
        {
            return ListHelper.Select<T>(this, (QueryParameter<T>)parameter, indexes);
        }

        public IEnumerable<T> Select(QueryParameter<T> parameter)
        {
            return ListHelper.Select<T>(this, parameter, indexes);
        }

        IEnumerable ISelectable.Select(string property, CompareType comparer, object value)
        {
            return Select(property, comparer, value);
        }

        public IEnumerable<T> Select(IInvoker invoker, CompareType comparer, object value)
        {
            return Select(new QueryParameter<T>
            {
                Invoker = invoker,
                Comparer = comparer,
                Value = value
            });
        }

        public IEnumerable<T> Select(string property, CompareType comparer, object value)
        {
            return Select(new QueryParameter<T>
            {
                Invoker = Indexes.GetIndex(property)?.Invoker,
                Name = property,
                Comparer = comparer,
                Value = value
            });
        }

        public T SelectOne<K>(string property, K value)
        {
            if (indexes.GetIndex(property) is IListIndex<T, K> index)
            {
                return index.SelectOne(value);
            }
            return SelectOne(property, CompareType.Equal, value);
        }

        public T SelectOne(string property, object value)
        {
            return SelectOne(property, CompareType.Equal, value);
        }

        public T SelectOne(string property, CompareType comparer, object value)
        {
            var index = indexes.GetIndex(property);
            if (index != null)
            {
                return index.SelectOne(value);
            }
            return Find(property, comparer, value);
        }

        #endregion

        public virtual object NewItem()
        {
            return TypeHelper.CreateObject(type);
        }

        public virtual void Dispose()
        {
            //_Properties.Clear();
            type = null;
            comparer = null;
            searchComparer = null;
            propertyHandler = null;
            if (items != null)
            {
                items.Clear();
                items = null;
            }
        }

        public bool Disposed
        {
            get { return items == null; }
        }

        [XmlIgnore, Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
        public bool CheckUnique { get; set; } = true;

        public virtual void OnListChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
            OnPropertyChanged(nameof(SyncRoot));
        }

        public virtual void OnListChanged(NotifyCollectionChangedAction type, object item = null, int index = -1, int oldIndex = -1, object oldItem = null)
        {
            NotifyCollectionChangedEventArgs args = null;
            switch (type)
            {
                case NotifyCollectionChangedAction.Reset:
                    args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    args = new NotifyCollectionChangedEventArgs(type, item, index);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index);
                    break;
                case NotifyCollectionChangedAction.Move:
                    args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, index, oldIndex);
                    break;
            }
            OnListChanged(args);
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public virtual void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var item = (T)sender;
            OnItemPropertyChanged(item, -1, e);
        }

        public void OnItemPropertyChanged(T item, int index, PropertyChangedEventArgs e)
        {
            var lindex = indexes.GetIndex(e.PropertyName);
            if (lindex != null)
            {
                if (e is PropertyChangedDetailEventArgs details)
                {
                    lindex.Remove(item, details.OldValue);
                    lindex.Add(item, details.NewValue);
                }
                else
                {
                    lindex.Refresh(item);
                }
            }
            if (IsSorted)
            {
                if (index < 0)
                    index = items.IndexOf(item);
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
                    OnListChanged(NotifyCollectionChangedAction.Move, item, newindex, index);
                }
            }
            ItemPropertyChanged?.Invoke(item, e);
        }

        public bool IsFirst(T item)
        {
            return items.Count > 0 && items[0].Equals(item);
        }

        public bool IsLast(T item)
        {
            return items.Count > 0 && items[items.Count - 1].Equals(item);
        }

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
                OnListChanged(NotifyCollectionChangedAction.Reset);
            }
        }

        public virtual void InsertInternal(int index, T item)
        {
            if (item == null)
                return;

            indexes.AddItem(item);
            items.Insert(index, item);

            if (propertyHandler != null)
            {
                ((INotifyPropertyChanged)item).PropertyChanged += propertyHandler;
            }
        }

        void IList.Insert(int index, object item)
        {
            Insert(index, (T)item);
        }

        public virtual void Insert(int index, T item)
        {
            InsertInternal(index, item);
            OnListChanged(NotifyCollectionChangedAction.Add, item, index);
        }

        public virtual int AddInternal(T item)
        {
            lock (lockObject)
            {
                int index = GetIndexForAdding(item);
                if (index < 0)
                {
                    index = -index - 1;
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

        protected int GetIndexForAdding(T item)
        {
            if (comparer != null)
            {
                var index = ListHelper.BinarySearch(items, item, comparer);
                return CheckUnique ? index : -Math.Abs(index);
            }
            return CheckUnique
                ? Contains(item) ? IndexOf(item) : -(items.Count + 1)
                : -(items.Count + 1);
        }

        protected int GetIndexBySort(T item)
        {
            if (comparer != null)
            {
                return ListHelper.BinarySearch(items, item, comparer);
            }
            return Contains(item) ? IndexOf(item) : -(items.Count + 1);
        }

        int IList.Add(object item)
        {
            return Add((T)item);
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public virtual int Add(T item)
        {
            int index = AddInternal(item);
            if (index >= 0)
            {
                OnListChanged(NotifyCollectionChangedAction.Add, item, index);
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
                indexes.RemoveItem(item);
            }
            items.RemoveAt(index);
        }

        public void Remove(T item, int index)
        {
            RemoveInternal(item, index);
            OnListChanged(NotifyCollectionChangedAction.Remove, item, index);
        }

        public void Remove(object item)
        {
            Remove((T)item);
        }

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

        public void RemoveAt(int index)
        {
            Remove(items[index], index);
        }

        public virtual int IndexOf(object item)
        {
            return IndexOf((T)item);
        }

        public int IndexOf(T item)
        {
            //if (comparer != null)
            //    return ListHelper.BinarySearch(items, item, comparer);
            return items.IndexOf(item);
        }

        public T this[int index]
        {
            get { return items[index]; }
            set
            {
                T item = items[index];

                if (item.Equals(value))
                    return;
                var valueIndex = IndexOf(value);
                if (valueIndex >= 0)
                {
                    items.RemoveAt(valueIndex);
                    items.Insert(index, value);
                    OnListChanged(NotifyCollectionChangedAction.Move, value, index, valueIndex, item);
                }
                else
                {
                    RemoveInternal(item, index);
                    InsertInternal(index, value);
                    OnListChanged(NotifyCollectionChangedAction.Replace, value, index, index, item);
                }
            }
        }

        object IList.this[int index]
        {
            get { return GetItem(index); }
            set { this[index] = (T)value; }
        }

        public virtual object GetItem(int index)
        {
            return this[index];
        }

        public void ApplySortInternal(params string[] property)
        {
            var comparerList = new InvokerComparerList<T>();
            foreach (string column in property)
            {
                var direction = ListSortDirection.Ascending;
                if (column.EndsWith(" DESC", StringComparison.OrdinalIgnoreCase))
                    direction = ListSortDirection.Descending;
                var index = column.IndexOf(" ", StringComparison.Ordinal);
                comparerList.Add(new InvokerComparer<T>(index > 0 ? column.Substring(0, index) : column, direction));
            }
            ApplySortInternal(comparerList);
        }

        public void ApplySortInternal(string property, ListSortDirection direction)
        {
            ApplySortInternal(new InvokerComparer<T>(property, direction));
        }

        public virtual void ApplySortInternal(IComparer<T> comparer)
        {
            lock (lockObject)
            {
                this.comparer = comparer;
                UpdateSort();
            }
        }

        public void UpdateSort()
        {
            ListHelper.QuickSort<T>(items, comparer);
            //_items.Sort(comparer);
        }

        public void ApplySort(IComparer comparer)
        {
            ApplySort(comparer == null ? null
                      : comparer is IComparer<T> gcomparer ? gcomparer
                      : new ComparerWrapper<T>(comparer));
        }

        public void ApplySort(IComparer<T> comparer)
        {
            if (this.comparer != null && this.comparer.Equals(comparer))
                return;
            ApplySortInternal(comparer);
            OnListChanged(NotifyCollectionChangedAction.Reset);
        }

        public T Find(Query<T> query)
        {
            return Select(query).FirstOrDefault();
        }

        public T Find(PropertyInfo property, object value)
        {
            return Find(property.Name, CompareType.Equal, value);
        }

        public T Find(string property, CompareType comparer, object value)
        {
            return Select(property, comparer, value).FirstOrDefault();
        }

        public void RemoveSort()
        {
            comparer = null;
            Sort();
        }

        public void SortInternal()
        {
            ListHelper.QuickSort<T>(items, comparer);
        }

        public void Sort()
        {
            SortInternal();
            OnListChanged(NotifyCollectionChangedAction.Reset);
        }

        public void Sort(Comparison<T> comp)
        {
            items.Sort(comp);
        }

        public void Sort(IComparer<T> comp)
        {
            ListHelper.QuickSort<T>(items, comp);
        }

        public object GetPrev(object item)
        {
            int index = items.IndexOf((T)item);
            if (index <= 0)
                return null;
            return items[--index];
        }

        public object GetNext(object item)
        {
            int index = items.IndexOf((T)item);
            if (index < 0 || index >= Count - 1)
                return null;
            return items[++index];
        }

        public T GetFirst()
        {
            return items.Count == 0 ? default(T) : items[0];
        }

        public T GetLast()
        {
            return items.Count == 0 ? default(T) : items[items.Count - 1];
        }

        #region ICollection Members
        public bool Contains(object item)
        {
            return Contains((T)item);
        }

        public virtual bool Contains(T item)
        {
            if (item is IContainerNotifyPropertyChanged containered && propertyHandler != null)
            {
                return containered.Containers.Contains(this);
            }
            return items.Contains(item);
        }

        public void CopyTo(T[] array, int index)
        {
            items.CopyTo(0, array, index, Math.Min(array.Length - index, Count));
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ArrayList.Adapter(items).CopyTo(array, index);
        }

        #endregion

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ThreadSafeEnumerator<T>(items);
        }

        public void AddRangeInternal(IEnumerable<T> list)
        {
            foreach (T item in list)
            {
                AddInternal(item);
            }
        }

        public void AddRange(IEnumerable<T> list)
        {
            AddRangeInternal(list);
            OnListChanged(NotifyCollectionChangedAction.Reset);
        }

        public void RemoveRange(IEnumerable<T> toDelete)
        {
            foreach (T item in toDelete)
            {
                Remove(item);
            }
        }

        [Invoker(typeof(SelectableList<>), nameof(Disposed))]
        public class DisposedInvoker : Invoker<SelectableList<T>, bool>
        {
            public override string Name => nameof(SelectableList<T>.Disposed);

            public override bool CanWrite => false;

            public override bool GetValue(SelectableList<T> target) => target.Disposed;

            public override void SetValue(SelectableList<T> target, bool value) { }
        }

        [Invoker(typeof(SelectableList<>), nameof(IsFixedSize))]
        public class IsFixedSizeInvoker : Invoker<SelectableList<T>, bool>
        {
            public override string Name => nameof(SelectableList<T>.IsFixedSize);

            public override bool CanWrite => false;

            public override bool GetValue(SelectableList<T> target) => target.IsFixedSize;

            public override void SetValue(SelectableList<T> target, bool value) { }
        }

        [Invoker(typeof(SelectableList<>), nameof(IsReadOnly))]
        public class IsReadOnlyInvoker : Invoker<SelectableList<T>, bool>
        {
            public override string Name => nameof(SelectableList<T>.IsReadOnly);

            public override bool CanWrite => false;

            public override bool GetValue(SelectableList<T> target) => target.IsReadOnly;

            public override void SetValue(SelectableList<T> target, bool value) { }
        }

        [Invoker(typeof(SelectableList<>), nameof(IsSorted))]
        public class IsSortedInvoker : Invoker<SelectableList<T>, bool>
        {
            public override string Name => nameof(SelectableList<T>.IsSorted);

            public override bool CanWrite => false;

            public override bool GetValue(SelectableList<T> target) => target.IsSorted;

            public override void SetValue(SelectableList<T> target, bool value) { }
        }

        [Invoker(typeof(SelectableList<>), nameof(Comparer))]
        public class ComparerInvoker : Invoker<SelectableList<T>, IComparer<T>>
        {
            public override string Name => nameof(SelectableList<T>.Comparer);

            public override bool CanWrite => false;

            public override IComparer<T> GetValue(SelectableList<T> target) => target.Comparer;

            public override void SetValue(SelectableList<T> target, IComparer<T> value) { }
        }

        [Invoker(typeof(SelectableList<>), nameof(Count))]
        public class CountInvoker : Invoker<SelectableList<T>, int>
        {
            public override string Name => nameof(SelectableList<T>.Count);

            public override bool CanWrite => false;

            public override int GetValue(SelectableList<T> target) => target.Count;

            public override void SetValue(SelectableList<T> target, int value) { }
        }

        [Invoker(typeof(SelectableList<>), nameof(SyncRoot))]
        public class SyncRootInvoker : Invoker<SelectableList<T>, object>
        {
            public override string Name => nameof(SelectableList<T>.SyncRoot);

            public override bool CanWrite => false;

            public override object GetValue(SelectableList<T> target) => target.SyncRoot;

            public override void SetValue(SelectableList<T> target, object value) { }
        }
    }
}
