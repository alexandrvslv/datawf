using DataWF.Comparers;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections.Query.Evaluators;

public class CompareQConstObjectConstTupleFilter<V> : ParameterFilter<QTuple, V>
{
    public static readonly CompareQConstObjectConstTupleFilter<V> Instance = new();

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
            private readonly bool IsSuccess;
            private readonly IEnumerator<QTuple> ItemsEnumerator;

            public TupleEnumerator(IEnumerable<QTuple> items, IQCompareParameter compareParameter)
            {
                ItemsEnumerator = items.GetEnumerator();
                var item = (IQValue<V>)compareParameter.LeftItem;
                var value = ((IQValue)compareParameter.RightItem).Value;
                var evaluator = (ICompareEvaluator<V>)compareParameter.Evaluator;
                IsSuccess = evaluator.Eval(item.Value, value, (IUniversalComparer<V>)compareParameter.KeyComparer);
            }

            public QTuple Current => ItemsEnumerator.Current;

            object IEnumerator.Current => ItemsEnumerator.Current;

            public void Dispose() => ItemsEnumerator.Dispose();

            public bool MoveNext() => IsSuccess && ItemsEnumerator.MoveNext();

            public void Reset() => ItemsEnumerator.Reset();

            public readonly int Count()
            {
                int count = 0;
                while (IsSuccess && ItemsEnumerator.MoveNext())
                {
                    count++;
                }
                return count;
            }

            public readonly bool Contains(QTuple item, IEqualityComparer<QTuple> equalityComparer)
            {
                while (IsSuccess && ItemsEnumerator.MoveNext())
                {
                    if (equalityComparer.Equals(ItemsEnumerator.Current, item))
                        return true;
                }
                return false;
            }
        }
    }
}

