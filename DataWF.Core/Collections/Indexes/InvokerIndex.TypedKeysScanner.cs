using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections.Indexes;

public partial class InvokerIndex<T, K>
{
    public struct TypedKeysScanner : IFilteredCollection<T>
    {
        private readonly InvokerIndex<T, K> listIndex;
        private readonly IEnumerable<K> keys;

        public TypedKeysScanner(InvokerIndex<T, K> listIndex, IEnumerable<K> keys)
        {
            this.listIndex = listIndex;
            this.keys = keys;
        }

        public int Count => new Enumerator(listIndex, keys).GetCount();

        public bool Contains(object item)
        {
            return new Enumerator(listIndex, keys).Contains((T)item, listIndex.valueComparer);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Enumerator GetEnumerator()
        {
            return new Enumerator(listIndex, keys);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private OneArrayPointerEnumerator<T> entryEnumerator;
            private readonly IDictionary<K, ArrayPointerList<T>> dictionary;
            private readonly IEnumerator<K> keysEnumerator;
            public Enumerator(InvokerIndex<T, K> listIndex, IEnumerable<K> keys)
            {
                entryEnumerator = OneArrayPointerEnumerator<T>.Empty;
                dictionary = listIndex.store;
                keysEnumerator = keys.GetEnumerator();
            }

            public T Current => entryEnumerator.Current;

            object IEnumerator.Current => entryEnumerator.Current!;

            public void Dispose()
            {
                keysEnumerator.Dispose();
                entryEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                var result = false;
                while (!(result = entryEnumerator.MoveNext())
                    && keysEnumerator.MoveNext())
                {
                    if (dictionary.TryGetValue(keysEnumerator.Current, out var list))
                    {
                        entryEnumerator.Dispose();
                        entryEnumerator = list.GetEnumerator();
                    }
                }
                return result;
            }

            public readonly int GetCount()
            {
                int count = 0;
                while (entryEnumerator.MoveNext())
                {
                    if (dictionary.TryGetValue(keysEnumerator.Current, out var list))
                        count += list.Count;
                }
                return count;
            }

            public readonly bool Contains(T item, IComparer<T> comparer)
            {
                while (entryEnumerator.MoveNext())
                {
                    if (dictionary.TryGetValue(keysEnumerator.Current, out var list)
                        && list.BinarySearch(item, comparer) > -1)
                        return true;
                }
                return false;
            }

            public void Reset()
            {
                keysEnumerator.Reset();
                entryEnumerator.Dispose();
                entryEnumerator = OneArrayPointerEnumerator<T>.Empty;
            }
        }
    }
}


