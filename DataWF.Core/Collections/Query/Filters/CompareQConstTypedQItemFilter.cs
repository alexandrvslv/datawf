using DataWF.Comparers;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections.Query.Evaluators;

public class CompareQConstTypedQItemFilter<T, V> : ParameterFilter<T, V>
{
    public static readonly CompareQConstTypedQItemFilter<T, V> Instance = new();

    public override IFilteredCollection<T> Filter(IEnumerable<T> items, IQParameter parameter) =>
        new FilteredCollection(items, (IQCompareParameter)parameter);

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

        public Enumerator GetEnumerator() =>
            new(items, compareParameter);

        public struct Enumerator : IEnumerator<T>
        {
            private readonly V Item;
            private readonly IQItem<V> Value;
            private readonly ICompareEvaluator<V> Evaluator;
            private readonly IUniversalComparer<V> Comparer;
            private readonly IEnumerator<T> ItemsEnumerator;

            public Enumerator(IEnumerable<T> items, IQCompareParameter compareParameter)
            {
                ItemsEnumerator = items.GetEnumerator();
                Item = ((IQValue<V>)compareParameter.LeftItem).Value;
                Value = (IQItem<V>)compareParameter.RightItem;
                Evaluator = (ICompareEvaluator<V>)compareParameter.Evaluator;
                Comparer = (IUniversalComparer<V>)compareParameter.KeyComparer;
            }

            public T Current => ItemsEnumerator.Current;

            object IEnumerator.Current => ItemsEnumerator.Current;

            public void Dispose() => ItemsEnumerator.Dispose();

            public bool MoveNext()
            {
                var result = false;
                while ((result = ItemsEnumerator.MoveNext())
                    && !Evaluator.Eval(Item,
                                          Value.GetValue(ItemsEnumerator.Current), Comparer))
                { }
                return result;
            }

            public void Reset() => ItemsEnumerator.Reset();
            public readonly int Count()
            {
                int count = 0;
                while (ItemsEnumerator.MoveNext()
                    && Evaluator.Eval(Item,
                                        Value.GetValue(ItemsEnumerator.Current), Comparer))
                {
                    count++;
                }
                return count;
            }

            public bool Contains(T item, IEqualityComparer<T> equalityComparer)
            {
                while (ItemsEnumerator.MoveNext()
                    && Evaluator.Eval(Item,
                                        Value.GetValue(ItemsEnumerator.Current), Comparer))
                {
                    if (equalityComparer.Equals(ItemsEnumerator.Current, item))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}

