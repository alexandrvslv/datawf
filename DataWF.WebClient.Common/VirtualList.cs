using DataWF.WebClient.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class VirtualList<T> : DefaultItem, IFilterable<T>, IVirtualList
    {
        private HttpPageSettings pages = new HttpPageSettings { Mode = HttpPageMode.List, PageSize = 20 };
        private Dictionary<int, List<T>> cache = new Dictionary<int, List<T>>();
        private IModelView modelView;
        private int processingGet;
        private Query<T> filterQuery;

        public VirtualList()
        {
            //Pages.PageSize = Math.Max(20, ((int)Math.Ceiling(Math.Ceiling(canvasView.Height / rowHeight) / 10.0)) * 10);
        }

        public T this[int index]
        {
            get => GetItem(index);
            set => throw new NotSupportedException();
        }

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (T)value;
        }

        public Query<T> FilterQuery
        {
            get => filterQuery;
            set
            {
                if (filterQuery != value)
                {
                    if (filterQuery != null)
                    {
                        filterQuery.ParametersChanged -= OnFilterQueryChanged;
                        filterQuery.OrdersChanged -= OnFilterQueryChanged;
                    }
                    filterQuery = value;
                    if (filterQuery != null)
                    {
                        filterQuery.ParametersChanged += OnFilterQueryChanged;
                        filterQuery.OrdersChanged += OnFilterQueryChanged;
                    }
                }
            }
        }

        IQuery IFilterable.FilterQuery
        {
            get => FilterQuery;
            set => FilterQuery = (Query<T>)value;
        }

        public IEnumerable Source { get; set; }

        public bool IsFixedSize => false;

        public bool IsReadOnly => true;

        public int Count => Pages.ListCount;

        public bool IsSynchronized => true;

        public object SyncRoot => cache;

        public IModelView ModelView
        {
            get => modelView;
            set
            {
                if (modelView != value)
                {
                    modelView = value;
                }
            }
        }

        public HttpPageSettings Pages
        {
            get => pages;
            set => pages = value;
        }

        public int PageSize
        {
            get => Pages.PageSize;
            set
            {
                Pages.PageSize = value;
                Clear();
            }
        }

        public IEnumerable<IFilterable> Views => throw new NotImplementedException();

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler ItemPropertyChanged;
        public event EventHandler FilterChanged;

        public IEnumerable<TT> GetHandlers<TT>() => TypeHelper.GetHandlers<TT>(CollectionChanged);

        public T GetItem(int index)
        {
            var pageIndex = index / Pages.PageSize;
            var itemIndex = index % Pages.PageSize;
            if (!cache.TryGetValue(pageIndex, out var items))
            {
                if (ModelView != null)
                {
                    _ = ProcessGet(index, pageIndex);
                }

                return default(T);
            }
            return items != null ? items[itemIndex] : default(T);
        }

        private async ValueTask ProcessGet(int index, int pageIndex)
        {
            if (Interlocked.CompareExchange(ref processingGet, 1, 0) == 0)
            {
                try
                {
                    cache[pageIndex] = null;
                    pages.PageIndex = pageIndex;
                    pages.ListFrom = pages.PageIndex * pages.PageSize;
                    pages.ListTo = (pages.ListFrom + pages.PageSize) - 1;

                    int i = pages.ListFrom;
                    var result = (await ModelView.Get(pages).ConfigureAwait(false)).Cast<T>().ToList();
                    cache[pageIndex] = result;

                    OnCollectionChanged(NotifyCollectionChangedAction.Reset);
                }
                catch (Exception ex)
                {
                    cache.Remove(pages.PageIndex);
                    Helper.OnException(ex);
                }
                finally
                {
                    Interlocked.Decrement(ref processingGet);
                }
            }
        }

        public int Add(object value)
        {
            Add((T)value);
            return Count - 1;
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            cache.Clear();
            pages.ListCount = 1;
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        public bool Contains(object value)
        {
            return Contains((T)value);
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(Array array, int index)
        {
            CopyTo((T[])array, index);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var page in cache)
            {
                if (page.Value == null)
                    continue;
                foreach (var item in page.Value)
                {
                    yield return item;
                }
            }
        }

        public int IndexOf(object value)
        {
            return IndexOf((T)value);
        }

        public int IndexOf(T item)
        {
            foreach (var entry in cache)
            {
                var inPageIndex = entry.Value?.IndexOf(item) ?? -1;
                if (inPageIndex > -1)
                {
                    return entry.Key * pages.PageSize + inPageIndex;
                }
            }
            return -1;
        }

        public void Insert(int index, object value)
        {
            Insert(index, (T)value);
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public void Remove(object value)
        {
            Remove((T)value);
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        private void OnFilterQueryChanged(object sender, EventArgs e)
        {
            UpdateFilter();
        }

        public void UpdateFilter()
        {
            if (!filterQuery.Suspending)
            {
                Clear();
                FilterChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void SetCollection(IList baseCollection)
        {
            Source = baseCollection;
        }

        public void OnCollectionChanged(NotifyCollectionChangedAction type, object item = null, int index = -1, int oldIndex = -1, object oldItem = null)
        {
            CollectionChanged?.Invoke(this, ListHelper.GenerateArgs(type, item, index, oldIndex, oldItem));
        }

        public void OnItemPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            ItemPropertyChanged?.Invoke(sender, args);
        }

        public void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

        public void OnSourceItemChanged(object sender, PropertyChangedEventArgs e)
        {
        }
    }
}