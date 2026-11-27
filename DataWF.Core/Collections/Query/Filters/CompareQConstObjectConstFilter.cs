using DataWF.Comparers;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections.Query.Evaluators;

public class CompareQConstObjectConstFilter<T, V> : ParameterFilter<T, V>
{
    public static readonly CompareQConstObjectConstFilter<T, V> Instance = new();

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
            private readonly bool IsSuccess;
            private readonly IEnumerator<T> ItemsEnumerator;

            public Enumerator(IEnumerable<T> items, IQCompareParameter compareParameter)
            {
                ItemsEnumerator = items.GetEnumerator();
                var item = (IQValue<V>)compareParameter.LeftItem;
                var value = ((IQValue)compareParameter.RightItem).Value;
                var evaluator = compareParameter.Comparer.GetEvaluator<V>();
                IsSuccess = evaluator.Eval(item.Value, value, (IUniversalComparer<V>)compareParameter.KeyComparer);
            }

            public T Current => ItemsEnumerator.Current;

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

            public readonly bool Contains(T item, IEqualityComparer<T> equalityComparer)
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

