using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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

        public SelectableList(int capacity)
        {
            items = new List<T>();
            type = typeof(T);
            if (TypeHelper.IsInterface(type, typeof(INotifyPropertyChanged)))
            {
                propertyHandler = new PropertyChangedEventHandler(OnPropertyChanged);
            }
        }

        public SelectableList() : this(0)
        { }

        public SelectableList(IEnumerable<T> items, IComparer<T> comparer = null) : this(items.Count())
        {
            this.comparer = comparer;
            AddRangeInternal(items);
        }

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
            get { return true; }
            set { }
        }

        [Browsable(false)]
        public object SyncRoot
        {
            get { return items; }
        }

        #region Use Index
        IEnumerable ISelectable.Select(Query query)
        {
            return ListHelper.Select<T>(items, query, indexes);
        }

        public IEnumerable<T> Select(Query query)
        {
            return ListHelper.Select<T>(items, query, indexes);
        }

        IEnumerable ISelectable.Select(QueryParameter parameter)
        {
            return ListHelper.Select<T>(items, parameter, indexes);
        }

        public IEnumerable<T> Select(QueryParameter parameter)
        {
            return ListHelper.Select<T>(items, parameter, indexes);
        }

        IEnumerable ISelectable.Select(string property, CompareType comparer, object value)
        {
            return Select(property, comparer, value);
        }

        public IEnumerable<T> Select(string property, CompareType comparer, object value)
        {
            return Select(new QueryParameter
            {
                Type = typeof(T),
                Comparer = comparer,
                Value = value,
                Property = property
            });
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

        public virtual void OnListChanged(ListChangedType type, int newIndex = -1, int oldIndex = -1, object sender = null, string property = null)
        {
            ListChanged?.Invoke(this, new ListPropertyChangedEventArgs(type, newIndex, oldIndex) { Property = property, Sender = sender });
        }

        public virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var lindex = indexes.GetIndex(e.PropertyName);
            if (lindex != null)
            {
                lindex.Remove((T)sender);
                lindex.Add((T)sender);
            }
            int index = items.IndexOf((T)sender);
            if (IsSorted)
            {
                int newindex = GetIndexBySort((T)sender);
                if (index != newindex)
                {
                    if (newindex > index)
                        newindex--;
                    items.RemoveAt(index);
                    items.Insert(newindex, (T)sender);
                    OnListChanged(ListChangedType.ItemMoved, newindex, index, sender, e.PropertyName);
                    return;
                }
            }
            if (index >= 0)
            {
                OnListChanged(ListChangedType.ItemChanged, index, -1, sender, e.PropertyName);
            }
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
            if (propertyHandler != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item is IContainerNotifyPropertyChanged && ((IContainerNotifyPropertyChanged)item).Container == this)
                    {
                        ((IContainerNotifyPropertyChanged)item).Container = null;
                    }
                    else
                    {
                        ((INotifyPropertyChanged)item).PropertyChanged -= propertyHandler;
                    }
                }
            }
            indexes.Clear();
            items.Clear();
        }

        public virtual void Clear()
        {
            if (items.Count > 0)
            {
                ClearInternal();
                OnListChanged(ListChangedType.Reset);
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
                if (item is IContainerNotifyPropertyChanged)
                {
                    if (((IContainerNotifyPropertyChanged)item).Container == null)
                    {
                        ((IContainerNotifyPropertyChanged)item).Container = this;
                        return;
                    }
                    if (((IContainerNotifyPropertyChanged)item).Container == this)
                    {
                        return;
                    }
                }
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
            OnListChanged(ListChangedType.ItemAdded, index, -1, item);
        }

        public virtual int AddInternal(T item)
        {
            int index = GetIndexBySort(item);
            InsertInternal(index, item);
            return index;
        }

        protected int GetIndexBySort(T item)
        {
            if (comparer != null)
            {
                //int index = _items.BinarySearch(item, _comparer);
                int index = ListHelper.BinarySearch(items, item, comparer);
                if (index < 0)
                    index = -index - 1;

                if (index > items.Count)
                    index = items.Count;

                return index;
            }
            return items.Count;
        }

        int IList.Add(object item)
        {
            Add((T)item);
            return IndexOf(item);
        }

        public virtual void Add(T item)
        {
            int index = AddInternal(item);
            if (index >= 0)
            {
                OnListChanged(ListChangedType.ItemAdded, index, -1, item);
            }
        }

        public void RemoveInternal(T item, int index)
        {
            if (propertyHandler != null)
            {
                if (item is IContainerNotifyPropertyChanged && ((IContainerNotifyPropertyChanged)item).Container == this)
                {
                    ((IContainerNotifyPropertyChanged)item).Container = null;
                }
                else
                {
                    ((INotifyPropertyChanged)item).PropertyChanged -= propertyHandler;
                }
            }
            indexes.RemoveItem(item);
            items.RemoveAt(index);
        }

        public void Remove(T item, int index)
        {
            RemoveInternal(item, index);
            OnListChanged(ListChangedType.ItemDeleted, -1, index, item);
        }

        public void Remove(object item)
        {
            Remove((T)item);
        }

        public virtual bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index == -1)
                return false;
            Remove(item, index);
            return true;
        }

        public void RemoveAt(int index)
        {
            Remove(items[index], index);
        }

        public int IndexOf(object item)
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

                RemoveInternal(item, index);
                InsertInternal(index, value);
            }
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
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
                comparerList.Comparers.Add(new InvokerComparer<T>(index > 0 ? column.Substring(0, index) : column, direction));
            }
            ApplySortInternal(comparerList);
        }

        public void ApplySortInternal(string property, ListSortDirection direction)
        {
            ApplySortInternal(new InvokerComparer<T>(property, direction));
        }

        public virtual void ApplySortInternal(IComparer<T> comparer)
        {
            this.comparer = comparer;
            ListHelper.QuickSort<T>(items, this.comparer);
            //_items.Sort(comparer);
        }

        public void ApplySort(IComparer comparer)
        {
            ApplySort(comparer == null
                      ? null
                      : comparer is IComparer<T>
                      ? (IComparer<T>)comparer
                      : new InvokerComparer<T>(comparer));
        }

        public void ApplySort(IComparer<T> comparer)
        {
            if (this.comparer != null && this.comparer.Equals(comparer))
                return;
            ApplySortInternal(comparer);
            OnListChanged(ListChangedType.Reset);
        }

        public T Find(Query query)
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

        public event ListChangedEventHandler ListChanged;

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
            OnListChanged(ListChangedType.Reset);
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
            return items.Contains(item);
        }

        public void CopyTo(T[] array, int index)
        {
            items.CopyTo(array, index);
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
                AddInternal(item);
        }

        public void AddRange(IEnumerable<T> list)
        {
            AddRangeInternal(list);
            OnListChanged(ListChangedType.Reset, -1);
        }
    }
}
