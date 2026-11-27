using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DataWF.Collections;

/// <summary>
/// Потокобезопастный список на основе массивов пула
/// </summary>
/// <typeparam name="T"></typeparam>
public class ArrayPointerList<T> : IList<T>, ICollection<T>, IReadOnlyList<T>, ISorted<T>
{
    private T? single;
    private ArrayPointer<T>? items;
    private int count;

    public ArrayPointerList()
    { }

    public ArrayPointerList(int capacity = 0) => Capacity = capacity;

    public ArrayPointerList(T item) : this()
    {
        single = item;
        count = 1;
    }

    ~ArrayPointerList()
    {
        items?.Unsubscribe();
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => count;
    }

    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => items?.Length ?? 1;
        set => Reallock(value);
    }

    public bool IsSynchronized => true;

    public object? SyncRoot => null;

    public bool IsReadOnly => false;

    public T this[int index]
    {
        get => items != null ? items[index] : single!;
        set
        {
            if (items != null)
                items[index] = value;
            else
                single = value;
        }
    }

    public void Add(T item)
    {
        CheckReallock();
        if (count == 0)
        {
            single = item;
            Interlocked.Increment(ref count);
        }
        else
            items[Interlocked.Increment(ref count)] = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckReallock()
    {
        if ((uint)count >= (uint)Capacity)
        {
            Reallock();
        }
    }

    private void Reallock()
    {
        var capacity = Math.Max(count, 2) * 2;
        Reallock(capacity);
    }

    private void Reallock(int capacity)
    {
        var temp = new ArrayPointer<T>(capacity);
        if (items != null)
            items.AsSpan(0, count).CopyTo(temp.AsSpan());
        else if (count > 0)
            temp[0] = single;
        var swap = items;
        items = temp;

        swap?.Unsubscribe();
    }

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index > -1)
        {
            RemoveAt(index);
            return true;
        }
        return false;
    }

    public void RemoveAt(int index)
    {
        Interlocked.Decrement(ref count);
        if (items != null && (uint)index < (uint)count)
        {
            items.AsSpan(index + 1, count - index).CopyTo(items.AsSpan(index, count - index));
        }
        //items[_count] = default(T);
    }

    public void Insert(int index, T item)
    {
        CheckReallock();
        if ((uint)index < (uint)count)
        {
            var copyCount = count - index;
            items.AsSpan(index, copyCount).CopyTo(items.AsSpan(index + 1, copyCount));
        }
        Interlocked.Increment(ref count);
        items.Array[index] = item;
    }

    public int IndexOf(T item)
    {
        return items != null
            ? Array.IndexOf(items.Array, item, 0, (int)count)
            : EqualityComparer<T>.Default.Equals(item, single) ? 0 : -1;
    }

    public void Clear()
    {
        items?.Clear(0, count);
        count = 0;
        single = default(T);
    }

    public bool Contains(T item) => IndexOf(item) > -1;

    public void CopyTo(T[] array, int arrayIndex) => items.AsSpan(0, count).CopyTo(array.AsSpan(arrayIndex));

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    public OneArrayPointerEnumerator<T> GetEnumerator() => count == 0
        ? OneArrayPointerEnumerator<T>.Empty
        : items == null
            ? new OneArrayPointerEnumerator<T>(single!)
            : new OneArrayPointerEnumerator<T>(items, count);

    public int BinarySearch(T item, IComparer<T> comparer)
    {
        if (items != null)
        {
            return Array.BinarySearch(items.Array, 0, count, item, comparer);
            //ListHelper.BinarySearch(items, 0, _count - 1, item, comparer, false);//
        }

        var result = comparer.Compare(item, single);

        return result == 0 ? 0 : result > 0 ? -2 : -1;
    }

    public void Sort(IComparer<T> comparer)
    {
        if (items != null)
            Array.Sort(items.Array, 0, count, comparer);
    }

    public void AddRange(IEnumerable<T> enumerable)
    {
        foreach (var item in enumerable)
            Add(item);
    }

    public T FirstOrDefault()
    {
        return items == null
            ? single!
            : count > 0 ? items[0] : default!;
    }

    public ReadOnlySpan<T> AsReadOnlySpan()
    {
        return items == null
            ? new ReadOnlySpan<T>(ref single!)
            : items.AsSpan(0, count);
    }

    public Span<T> AsSpan()
    {
        return items == null
            ? new Span<T>(ref single!)
            : items.AsSpan(0, count);
    }

    public Span<T> AsSpan(int start, int count)
    {
        return items == null
            ? new Span<T>(ref single!)
            : items.AsSpan(start, count);
    }

}
