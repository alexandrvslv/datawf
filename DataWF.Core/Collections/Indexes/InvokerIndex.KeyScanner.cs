using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections.Indexes;

public partial class InvokerIndex<T, K>
{
    public struct KeyScanner : IFilteredCollection<T>
    {
        private readonly InvokerIndex<T, K> listIndex;
        private readonly K key;

        public KeyScanner(InvokerIndex<T, K> listIndex, K key)
        {
            this.listIndex = listIndex;
            this.key = key;
        }

        public int Count
        {
            get
            {
                using var enumerator = new Enumerator(listIndex, key);
                return enumerator.GetCount();
            }
        }

        public bool Contains(object item)
        {
            using var enumerator = new Enumerator(listIndex, key);
            return enumerator.Contains((T)item, listIndex.valueComparer);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Enumerator GetEnumerator() => new(listIndex, key);

        public struct Enumerator : IEnumerator<T>
        {
            private OneArrayPointerEnumerator<T> entryEnumerator;
            private readonly ArrayPointerList<T>? list;
            public Enumerator(InvokerIndex<T, K> listIndex, K key)
            {
                this.list = listIndex.store.TryGetValue(key, out var list) ? list : null;
                entryEnumerator = list?.GetEnumerator() ?? OneArrayPointerEnumerator<T>.Empty;
            }

            public T Current => entryEnumerator.Current;

            object IEnumerator.Current => entryEnumerator.Current!;

            public void Dispose() => entryEnumerator.Dispose();

            public bool MoveNext() => entryEnumerator.MoveNext();

            public readonly int GetCount() => list?.Count ?? 0;

            public readonly bool Contains(T item, IComparer<T> comparer) => (list?.BinarySearch(item, comparer) ?? -1) > -1;

            public void Reset() => entryEnumerator.Reset();
        }
    }
}

