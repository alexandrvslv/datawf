using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using DataWF.Invokers;

namespace DataWF.Comparers;

/// <summary>
/// Comparer list. for compound sorting and grouping
/// </summary>
public class InvokerComparerList : List<IComparer>, IComparerList
{
    public InvokerComparerList() : this(new List<IComparer>())
    { }

    public InvokerComparerList(IEnumerable<IComparer> comparers)
        : base(comparers)
    { }

    public virtual int Compare(object? x, object? y)
    {
        foreach (var comparer in this)
        {
            int retval = comparer.Compare(x, y);
            if (retval != 0)
                return retval;
        }
        if (x is IComparable compareable)
            return compareable.CompareTo(y);
        return x?.GetHashCode().CompareTo(y?.GetHashCode() ?? 0) ?? 0;
    }

    public override bool Equals(object? obj)
    {
        if (obj is InvokerComparerList list)
        {
            if (list.Count == Count)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (!list[i].Equals(this[i]))
                        return false;
                }
                return true;
            }
            return false;
        }
        return ReferenceEquals(this, obj);
    }

    public override int GetHashCode() => base.GetHashCode();
}

public class InvokerComparerList<T> : UniversalComparer<T>, IComparerList<T>, IEnumerable<UniversalComparer<T>>, IEnumerable<IComparer<T>>
{
    private List<UniversalComparer<T>> items = new();
    private UniversalComparer<T> comparer = UniversalComparer<T>.Default;

    public InvokerComparerList()
    { }

    public InvokerComparerList(IEnumerable<UniversalComparer<T>> comparers)
    {
        items.AddRange(comparers);
    }

    public override bool DefaultEquality => items.Count == 1 && items[0].DefaultEquality;

    public override bool DefaultOrder => items.Count == 1 && items[0].DefaultOrder;

    public int Count => items.Count;


    public UniversalComparer<T> this[int index]
    {
        get => items[index];
        set => items[index] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Compare(T? x, T? y)
    {
        var span = CollectionsMarshal.AsSpan(items);
        for (int i = 0; i < span.Length; i++)
        {
            int retval = span[i].Compare(x, y);
            if (retval != 0)
                return retval;
        }
        return span.IsEmpty ? comparer.Compare(x, y) : 0;
    }

    public override bool Equals(T? x, T? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        var span = CollectionsMarshal.AsSpan(items);
        if (span.IsEmpty)
            return comparer.Equals(x, y);

        for (int i = 0; i < span.Length; i++)
        {
            if (!span[i].Equals(x, y))
                return false;
        }
        return true;
    }

    public override int GetHashCode([DisallowNull] T obj)
    {
        var span = CollectionsMarshal.AsSpan(items);
        if (span.IsEmpty)
            return comparer.GetHashCode(obj);

        var hash = new HashCode();
        for (int i = 0; i < span.Length; i++)
            hash.Add(span[i].GetHashCode(obj));
        return hash.ToHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is InvokerComparerList<T> otherComparer)
        {
            return Equals(otherComparer);
        }
        return ReferenceEquals(this, obj);
    }

    public bool Equals(InvokerComparerList<T> otherComparer)
    {
        var list = otherComparer.items;
        if (list.Count == items.Count)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (!list[i].Equals(items[i]))
                    return false;
            }
            return true;
        }
        return false;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        var span = CollectionsMarshal.AsSpan(items);
        for (int i = 0; i < span.Length; i++)
            hash.Add(span[i]);
        return hash.ToHashCode();
    }

    public void Add(UniversalComparer<T> comparer) => items.Add(comparer);
    
    public void Add(IInvoker invoker) => Add((UniversalComparer<T>)invoker.CreateComparer());

    public List<UniversalComparer<T>>.Enumerator GetEnumerator() => items.GetEnumerator();

    IEnumerator<UniversalComparer<T>> IEnumerable<UniversalComparer<T>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<IComparer<T>> IEnumerable<IComparer<T>>.GetEnumerator() => GetEnumerator();
}

