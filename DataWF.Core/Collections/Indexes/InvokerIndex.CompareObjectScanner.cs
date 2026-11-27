using DataWF.Comparers;
using DataWF.Collections.Query.Evaluators;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections.Indexes;

public partial class InvokerIndex<T, K>
{
    public struct CompareObjectScanner : IFilteredCollection<T>
    {
        private readonly InvokerIndex<T, K> listIndex;
        private readonly ICompareEvaluator<K> compare;
        private readonly object key;

        public CompareObjectScanner(InvokerIndex<T, K> listIndex, ICompareEvaluator<K> compare, object key)
        {
            this.listIndex = listIndex;
            this.compare = compare;
            this.key = key;
        }

        public int Count => new Enumerator(listIndex, compare, key).GetCount();

        public bool Contains(object item)
        {
            return new Enumerator(listIndex, compare, key).Contains((T)item, listIndex.valueComparer);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Enumerator GetEnumerator() => new(listIndex, compare, key);

        public struct Enumerator : IEnumerator<T>
        {
            private OneArrayPointerEnumerator<T> entryEnumerator;
            private readonly IEnumerator<KeyValuePair<K, ArrayPointerList<T>>> dictionaryEnumerator;
            private readonly IUniversalComparer<K> keyComparer;
            private readonly ICompareEvaluator<K> compare;
            private readonly object key;

            public Enumerator(InvokerIndex<T, K> listIndex, ICompareEvaluator<K> compare, object key)
            {
                entryEnumerator = OneArrayPointerEnumerator<T>.Empty;
                dictionaryEnumerator = listIndex.store.GetEnumerator();
                keyComparer = listIndex.keyComparer;
                this.compare = compare;
                this.key = key;
            }

            public T Current => entryEnumerator.Current;

            object IEnumerator.Current => entryEnumerator.Current!;

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
                    if (compare.Eval(entry.Key, key, keyComparer))
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
                    && compare.Eval(dictionaryEnumerator.Current.Key, key, keyComparer))
                {
                    count += dictionaryEnumerator.Current.Value.Count;
                }
                return count;
            }

            public readonly bool Contains(T item, IComparer<T> comparer)
            {
                while (dictionaryEnumerator.MoveNext()
                    && compare.Eval(dictionaryEnumerator.Current.Key, key, keyComparer))
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

