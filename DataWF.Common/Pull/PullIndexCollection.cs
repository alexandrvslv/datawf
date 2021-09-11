using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Common
{
    public struct PullIndexCollection<T> : IPullIndexCollection<T> where T : class, IPullHandler
    {
        public static readonly PullIndexCollection<T> Empty = new PullIndexCollection<T>(Enumerable.Empty<ThreadSafeList<T>>(), null);

        private readonly IEnumerable<ThreadSafeList<T>> items;
        private readonly IComparer<T> comparer;
        private int? count;

        public PullIndexCollection(IEnumerable<ThreadSafeList<T>> items, IComparer<T> comparer)
        {
            this.items = items;
            this.comparer = comparer;
            this.count = null;
        }

        public int Count => count ?? (count = items.Sum(p => p.Count)).Value;

        public bool Contains(object item) => Contains((T)item);

        public bool Contains(T item)
        {
            var comparer = this.comparer;
            return items.Any(p => p.BinarySearch(item, comparer) > -1);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var list in items)
            {
                foreach (var item in list)
                {
                    yield return item;
                }
            }
        }
    }



}