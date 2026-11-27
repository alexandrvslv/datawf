using DataWF.Comparers;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections.Query.Evaluators;

public class CompareQIndexTypedConstTupleFilter<V> : ParameterFilter<QTuple, V>
{
    public static readonly CompareQIndexTypedConstTupleFilter<V> Instance = new();

    public override IFilteredCollection<QTuple> Filter(IEnumerable<QTuple> items, IQParameter parameter) =>
        new TupleFilteredCollection(items, (IQCompareParameter)parameter);
    public struct TupleFilteredCollection : IFilteredCollection<QTuple>
    {
        private readonly IEnumerable<QTuple> items;
        private readonly IQCompareParameter compareParameter;

        public TupleFilteredCollection(IEnumerable<QTuple> items, IQCompareParameter compareParameter)
        {
            this.items = items;
            this.compareParameter = compareParameter;
        }

        public int Count => GetEnumerator().Count();

        public bool Contains(object item) => GetEnumerator().Contains((QTuple)item, DefaultEqualityComparer<QTuple>.Default);

        IEnumerator<QTuple> IEnumerable<QTuple>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TupleEnumerator GetEnumerator() =>
            new(items, compareParameter);
        public struct TupleEnumerator : IEnumerator<QTuple>
        {
            private readonly IEnumerator<QTuple> ItemsEnumerator;
            private readonly IQIndex<V> qItem;
            private readonly IFilteredCollection IndexCollection;

            public TupleEnumerator(IEnumerable<QTuple> items, IQCompareParameter compareParameter)
            {
                ItemsEnumerator = items.GetEnumerator();
                qItem = (IQIndex<V>)compareParameter.LeftItem;
                var value = ((IQValue<V>)compareParameter.RightItem).Value;
                IndexCollection = qItem.Index.Select(compareParameter.Comparer, value);
            }

            public QTuple Current => ItemsEnumerator.Current;

            object IEnumerator.Current => ItemsEnumerator.Current;

            public void Dispose() => ItemsEnumerator.Dispose();

            public bool MoveNext()
            {
                var result = false;
                while ((result = ItemsEnumerator.MoveNext())
                    && !IndexCollection.Contains(ItemsEnumerator.Current.Get(qItem.QTable)))
                { }
                return result;
            }

            public void Reset() => ItemsEnumerator.Reset();

            public readonly int Count()
            {
                var count = 0;
                while (ItemsEnumerator.MoveNext()
                    && IndexCollection.Contains(ItemsEnumerator.Current.Get(qItem.QTable)))
                {
                    count++;
                }
                return count;
            }

            public readonly bool Contains(QTuple item, IEqualityComparer<QTuple> equalityComparer)
            {
                while (ItemsEnumerator.MoveNext()
                    && IndexCollection.Contains(ItemsEnumerator.Current.Get(qItem.QTable)))
                {
                    if (equalityComparer.Equals(ItemsEnumerator.Current, item))
                        return true;
                }
                return false;
            }
        }
    }
}

