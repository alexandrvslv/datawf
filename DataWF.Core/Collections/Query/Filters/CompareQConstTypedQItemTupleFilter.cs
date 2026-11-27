using DataWF.Comparers;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections.Query.Evaluators;

public class CompareQConstTypedQItemTupleFilter<V> : ParameterFilter<QTuple, V>
{
    public static readonly CompareQConstTypedQItemTupleFilter<V> Instance = new();

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
            private readonly V Item;
            private readonly IQItem<V> Value;
            private readonly ICompareEvaluator<V> Evaluator;
            private readonly IUniversalComparer<V> Comparer;
            private readonly IEnumerator<QTuple> ItemsEnumerator;

            public TupleEnumerator(IEnumerable<QTuple> items, IQCompareParameter compareParameter)
            {
                ItemsEnumerator = items.GetEnumerator();
                Item = ((IQValue<V>)compareParameter.LeftItem).Value;
                Value = (IQItem<V>)compareParameter.RightItem;
                Evaluator = (ICompareEvaluator<V>)compareParameter.Evaluator;
                Comparer = (IUniversalComparer<V>)compareParameter.KeyComparer;
            }

            public QTuple Current => ItemsEnumerator.Current;

            object IEnumerator.Current => ItemsEnumerator.Current;

            public void Dispose() => ItemsEnumerator.Dispose();

            public bool MoveNext()
            {
                var result = false;
                while ((result = ItemsEnumerator.MoveNext())
                    && !Evaluator.Eval(Item,
                                          Value.GetValue<object>(ItemsEnumerator.Current.Get(Value.QTable)), Comparer))
                { }
                return result;
            }

            public void Reset() => ItemsEnumerator.Reset();

            public readonly int Count()
            {
                int count = 0;
                while (ItemsEnumerator.MoveNext()
                    && Evaluator.Eval(Item,
                                        Value.GetValue<object>(ItemsEnumerator.Current.Get(Value.QTable)), Comparer))
                {
                    count++;
                }
                return count;
            }

            public readonly bool Contains(QTuple item, IEqualityComparer<QTuple> equalityComparer)
            {
                while (ItemsEnumerator.MoveNext()
                    && Evaluator.Eval(Item,
                                        Value.GetValue<object>(ItemsEnumerator.Current.Get(Value.QTable)), Comparer))
                {
                    if (equalityComparer.Equals(ItemsEnumerator.Current, item))
                        return true;
                }
                return false;
            }
        }
    }
}

