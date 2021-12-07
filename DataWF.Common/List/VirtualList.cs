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
    public class VirtualList<T> : SelectableListView<T>, IVirtualList
    {
        private HttpPageSettings pages = new HttpPageSettings { Mode = HttpPageMode.List, PageSize = 20 };
        private Dictionary<int, List<T>> cache = new Dictionary<int, List<T>>();
        private IModelView modelView;
        private int processingGet;

        public VirtualList(Query<T> filter, IEnumerable source = null, bool handle = false) : base(filter, source, handle)
        {
            //Pages.PageSize = Math.Max(20, ((int)Math.Ceiling(Math.Ceiling(canvasView.Height / rowHeight) / 10.0)) * 10);
        }

        public IModelView ModelView
        {
            get => modelView;
            set
            {
                if (modelView != value)
                {
                    modelView = value;
                    ClearCache();
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
                ClearCache();
            }
        }

        public override int Count => Math.Max(Pages.ListCount, items.Count);

        public override T GetItemInternal(int index)
        {
            var pageIndex = index / Pages.PageSize;
            var itemIndex = index % Pages.PageSize;
            if (!cache.TryGetValue(pageIndex, out var items))
            {
                if (ModelView != null)
                {
                    _ = ProcessGet(pageIndex);
                }
            }
            if (items == null)
            {
                return index < this.items.Count ? this.items[index] : default(T);
            }
            return itemIndex < items.Count ? items[itemIndex] : default(T);
        }

        public async ValueTask<object> GetItemAsync(int index)
        {
            var pageIndex = index / Pages.PageSize;
            var itemIndex = index % Pages.PageSize;
            if (!cache.TryGetValue(pageIndex, out var items))
            {
                if (ModelView != null)
                {
                    await ProcessGet(pageIndex);
                }
            }
            if (items == null)
            {
                return index < this.items.Count ? this.items[index] : default(T);
            }
            return itemIndex < items.Count ? items[itemIndex] : default(T);
        }

        public async Task LoadAsync()
        {
            var result = (await ModelView.Get()).Cast<T>().ToList();
            for(var pageIndex = 0; pageIndex < Pages.PageCount; pageIndex++)
            {
                cache[pageIndex] = result.Skip(pageIndex * Pages.PageSize).Take(PageSize).ToList();
            }
        }

        private async ValueTask ProcessGet(int pageIndex, int? pageSize = null)
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
                    if (processingGet == 0)
                    {
                        cache.Remove(pageIndex);
                    }
                    else
                    {
                        cache[pageIndex] = result;
                        OnCollectionChanged(NotifyCollectionChangedAction.Reset);
                    }
                }
                catch (Exception ex)
                {
                    cache.Remove(pages.PageIndex);
                    Helper.OnException(ex);
                }
                finally
                {
                    Interlocked.Exchange(ref processingGet, 0);
                }
            }
        }

        public override void Clear()
        {
            ClearCache();
            base.Clear();
        }

        public override bool Contains(T item)
        {
            return base.Contains(item) || (IndexOf(item) >= 0);
        }

        public override OneListEnumerator<T> GetEnumerator()
        {
            var count = Count;
            return count == 0 
                ? OneListEnumerator<T>.Empty
                : new OneListEnumerator<T>(this, count);
        }

        public override int IndexOf(T item)
        {
            foreach (var entry in cache)
            {
                var inPageIndex = entry.Value?.IndexOf(item) ?? -1;
                if (inPageIndex > -1)
                {
                    return entry.Key * pages.PageSize + inPageIndex;
                }
            }
            return base.IndexOf(item);
        }

        protected override void Move(T item, int index)
        {
        }

        public override void ApplySortInternal(IComparer<T> comparer)
        {
            ClearCache();
            base.ApplySortInternal(comparer);
        }

        public override void UpdateFilter()
        {
            ClearCache();
            base.UpdateFilter();
        }

        private void ClearCache()
        {
            Interlocked.Exchange(ref processingGet, 0);
            cache.Clear();
            pages.ListCount = 1;
        }
    }
}