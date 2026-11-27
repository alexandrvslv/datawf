using DataWF.Collections.Indexes;
using DataWF.Comparers;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections.Query.Evaluators;

public class CompareQIndexObjectConstFilter<T, V> : ParameterFilter<T, V>
{
    public static readonly CompareQIndexObjectConstFilter<T, V> Instance = new();

    public override IFilteredCollection<T> Filter(IEnumerable<T> items, IQParameter parameter) =>
        GetIndexSource((IQCompareParameter)parameter) == items
            ? GetDirectIndexFilteredCollection((IQCompareParameter)parameter)
            : new FilteredCollection(items, (IQCompareParameter)parameter);

    private static IFilteredCollection<T> GetDirectIndexFilteredCollection(IQCompareParameter compareParameter)
    {
        return ((IIndexGetter<T, V>)((IQIndex<V>)compareParameter.LeftItem).Index)
                    .Select(compareParameter.Comparer, ((IQValue)compareParameter.RightItem).Value);
    }

    private static IEnumerable GetIndexSource(IQCompareParameter compareParameter)
    {
        return ((IQIndex<V>)compareParameter.LeftItem).Index.Source;
    }

    public struct FilteredCollection : IFilteredCollection<T>
    {
        private readonly IEnumerable<T> items;
        private readonly IQCompareParameter compareParameter;

        public FilteredCollection(IEnumerable<T> items, IQCompareParameter compareParameter)
        {
            this.items = items;
            this.compareParameter = compareParameter;
        }

        public int Count => GetEnumerator().Count();

        public bool Contains(object item) => GetEnumerator().Contains((T)item, DefaultEqualityComparer<T>.Default);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Enumerator GetEnumerator() => new(items, compareParameter);

        public struct Enumerator : IEnumerator<T>
        {
            private readonly IEnumerator<T> ItemsEnumerator;
            private readonly IFilteredCollection<T> IndexCollection;

            public Enumerator(IEnumerable<T> items, IQCompareParameter compareParameter)
            {
                ItemsEnumerator = items.GetEnumerator();
                var index = (IIndexGetter<T, V>)((IQIndex<V>)compareParameter.LeftItem).Index;
                var value = ((IQValue)compareParameter.RightItem).Value;
                IndexCollection = index.Select(compareParameter.Comparer, value);
            }

            public T Current => ItemsEnumerator.Current;

            object IEnumerator.Current => ItemsEnumerator.Current;

            public void Dispose() => ItemsEnumerator.Dispose();

            public bool MoveNext()
            {
                var result = false;
                while ((result = ItemsEnumerator.MoveNext())
                    && !IndexCollection.Contains(ItemsEnumerator.Current))
                { }
                return result;
            }

            public void Reset() => ItemsEnumerator.Reset();

            public readonly int Count()
            {
                int count = 0;
                while (ItemsEnumerator.MoveNext()
                    && IndexCollection.Contains(ItemsEnumerator.Current))
                {
                    count++;
                }
                return count;
            }

            public readonly bool Contains(T item, IEqualityComparer<T> equalityComparer)
            {
                while (ItemsEnumerator.MoveNext()
                    && IndexCollection.Contains(ItemsEnumerator.Current))
                {
                    if (equalityComparer.Equals(ItemsEnumerator.Current, item))
                        return true;
                }
                return false;
            }
        }
    }
}

