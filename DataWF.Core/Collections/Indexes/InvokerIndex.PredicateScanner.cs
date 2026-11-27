using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections.Indexes;

public partial class InvokerIndex<T, K>
{
    public struct PredicateScanner : IFilteredCollection<T>
    {
        private readonly InvokerIndex<T, K> listIndex;
        private readonly Predicate<K> predicate;
        public PredicateScanner(InvokerIndex<T, K> listIndex, Predicate<K> predicate)
        {
            this.listIndex = listIndex;
            this.predicate = predicate;
        }

        public int Count => new Enumerator(listIndex, predicate).GetCount();

        public bool Contains(object item)
        {
            return new Enumerator(listIndex, predicate).Contains((T)item, listIndex.valueComparer);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Enumerator GetEnumerator() => new Enumerator(listIndex, predicate);

        public struct Enumerator : IEnumerator<T>
        {
            private readonly IEnumerator<KeyValuePair<K, ArrayPointerList<T>>> dictionaryEnumerator;
            private readonly Predicate<K> predicate;
            private OneArrayPointerEnumerator<T> entryEnumerator;

            public Enumerator(InvokerIndex<T, K> listIndex, Predicate<K> predicate)
            {
                entryEnumerator = OneArrayPointerEnumerator<T>.Empty;
                dictionaryEnumerator = listIndex.store.GetEnumerator();
                this.predicate = predicate;
            }

            public T Current => entryEnumerator.Current;

            object IEnumerator.Current => entryEnumerator.Current;

            public void Dispose()
            {
                dictionaryEnumerator.Dispose();
                entryEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                var result = false;
                while (!(result = entryEnumerator.MoveNext())
                    && dictionaryEnumerator.MoveNext())
                {
                    var entry = dictionaryEnumerator.Current;
                    if (predicate(entry.Key))
                    {
                        entryEnumerator.Dispose();
                        entryEnumerator = entry.Value.GetEnumerator();
                    }
                }
                return result;
            }

            public readonly int GetCount()
            {
                int count = 0;
                while (dictionaryEnumerator.MoveNext()
                    && predicate(dictionaryEnumerator.Current.Key))
                {
                    count += dictionaryEnumerator.Current.Value.Count;
                }
                return count;
            }

            public readonly bool Contains(T item, IComparer<T> comparer)
            {
                while (dictionaryEnumerator.MoveNext()
                    && predicate(dictionaryEnumerator.Current.Key))
                {
                    if ((dictionaryEnumerator.Current.Value.BinarySearch(item, comparer) > -1))
                        return true;
                }
                return false;
            }

            public void Reset()
            {
                dictionaryEnumerator.Reset();
                entryEnumerator.Dispose();
                entryEnumerator = OneArrayPointerEnumerator<T>.Empty;
            }
        }
    }
}

