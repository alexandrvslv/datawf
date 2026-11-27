using DataWF.Collections.Query;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Collections.Indexes;

public struct InvokerIndexEnumerable<T> : IFilteredCollection<T>
{
    public static readonly InvokerIndexEnumerable<T> Empty = new(Enumerable.Empty<ArrayPointerList<T>>(), null);
    //TODO ReadOnlySequence
    private readonly IEnumerable<ArrayPointerList<T>> items;
    private readonly IComparer<T> comparer;
    private int? count;

    public InvokerIndexEnumerable(IEnumerable<ArrayPointerList<T>> items, IComparer<T> comparer)
    {
        this.items = items;
        this.comparer = comparer;
        this.count = null;
    }

    public int Count => count ??= items.Sum(p => p.Count);

    public bool Contains(object item) => Contains((T)item);

    public bool Contains(T item)
    {
        if (item == null)
            return false;
        var comparer = this.comparer;
        return items.Any(p => p.BinarySearch(item, comparer) > -1);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    public Enumerator GetEnumerator() => new Enumerator(items);

    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private readonly IEnumerator<ArrayPointerList<T>> itemsEnumerator;
        private OneArrayPointerEnumerator<T> currentEnumerator;

        public Enumerator(IEnumerable<ArrayPointerList<T>> items)
        {
            this.itemsEnumerator = items.GetEnumerator();
            currentEnumerator = OneArrayPointerEnumerator<T>.Empty;
        }
        public T Current => currentEnumerator.Current;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            itemsEnumerator.Dispose();
            currentEnumerator.Dispose();
        }

        public bool MoveNext()
        {
            var result = false;
            while (!(result = currentEnumerator.MoveNext())
                && itemsEnumerator.MoveNext())
            {
                currentEnumerator.Dispose();
                currentEnumerator = itemsEnumerator.Current.GetEnumerator();
            }
            return result;
        }

        public void Reset()
        {
            itemsEnumerator.Reset();
            currentEnumerator.Dispose();
            currentEnumerator = OneArrayPointerEnumerator<T>.Empty;
        }
    }
}

