using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DataWF.Invokers.Extensions;

namespace DataWF.Comparers;

public class SequenceComparer<T, TElement> : UniversalComparer<T>
    where T : IEnumerable<TElement>
{
    private readonly UniversalComparer<TElement> comparer = UniversalComparer<TElement>.Default;

    public override bool DefaultEquality => false;

    public override bool DefaultOrder => false;

    public override bool Equals(T? x, T? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        if (x is TElement[] xArr && y is TElement[] yArr)
        {
            return comparer.DefaultEquality
                ? xArr.AsSpan().SequenceEqual(yArr)
                : xArr.AsSpan().SequenceEqual(yArr, comparer);
        }

        if (x is List<TElement> xList && y is List<TElement> yList)
        {
            return comparer.DefaultEquality
                ? CollectionsMarshal.AsSpan(xList).SequenceEqual(CollectionsMarshal.AsSpan(yList))
                : CollectionsMarshal.AsSpan(xList).SequenceEqual(CollectionsMarshal.AsSpan(yList), comparer);
        }

        return x.SequenceEqual(y, comparer);
    }

    public override int GetHashCode(T obj)
    {
        var hash = new HashCode();

        if (obj is TElement[] arr)
        {
            if (comparer.DefaultEquality
            && !RuntimeHelpers.IsReferenceOrContainsReferences<TElement>()
            && Nullable.GetUnderlyingType(typeof(TElement)) is null)
            {
                AddBytesUnchecked(ref hash, arr);
                return hash.ToHashCode();
            }
            foreach (var item in arr)
                hash.Add(item, comparer);
            return hash.ToHashCode();
        }

        if (obj is List<TElement> list)
        {
            if (comparer.DefaultEquality
            && !RuntimeHelpers.IsReferenceOrContainsReferences<TElement>()
            && Nullable.GetUnderlyingType(typeof(TElement)) is null)
            {
                AddBytesUnchecked(ref hash, CollectionsMarshal.AsSpan(list));
                return hash.ToHashCode();
            }
            foreach (var item in CollectionsMarshal.AsSpan(list))
                hash.Add(item, comparer);
            return hash.ToHashCode();
        }

        foreach (var item in obj)
            hash.Add(item, comparer);

        return hash.ToHashCode();
    }

    private static void AddBytesUnchecked(ref HashCode hash, ReadOnlySpan<TElement> span)
    {
        if (span.IsEmpty) return;

        ref byte firstByte = ref Unsafe.As<TElement, byte>(ref MemoryMarshal.GetReference(span));
        int byteLength = checked(span.Length * Unsafe.SizeOf<TElement>());
        hash.AddBytes(MemoryMarshal.CreateReadOnlySpan(ref firstByte, byteLength));
    }

    public override int Compare(T? xEnumerable, T? yEnumerable)
    {
        if (ReferenceEquals(xEnumerable, yEnumerable)) return 0;
        if (xEnumerable is null) return -1;
        if (yEnumerable is null) return 1;


        // Fast-path arrays and List<T> with zero allocations
        if (xEnumerable is TElement[] xArr && yEnumerable is TElement[] yArr)
        {
            return comparer.DefaultOrder
                ? xArr.SequenceCompareTo(yArr)
                : xArr.SequenceCompareTo(yArr, comparer);
        }

        if (xEnumerable is List<TElement> xList && yEnumerable is List<TElement> yList)
        {
            return comparer.DefaultOrder
                ? CollectionsMarshal.AsSpan(xList).SequenceCompareTo(CollectionsMarshal.AsSpan(yList))
                : CollectionsMarshal.AsSpan(xList).SequenceCompareTo(CollectionsMarshal.AsSpan(yList), comparer);
        }
        return SequenceCompare(xEnumerable, yEnumerable);
    }

    public int SequenceCompare(IEnumerable<TElement> xEnumerable, IEnumerable<TElement> yEnumerable)
    {
        
        using var xEnumer = xEnumerable.GetEnumerator();
        using var yEnumer = yEnumerable.GetEnumerator();

        while (true)
        {
            bool xExist = xEnumer.MoveNext();
            bool yExist = yEnumer.MoveNext();

            if (!xExist) return yExist ? -1 : 0;
            if (!yExist) return 1;

            int result = comparer.Compare(xEnumer.Current, yEnumer.Current);
            if (result != 0) return result;
        }
    }

    public override bool Equals(T? x, object? y)
    {
        return Equals(x, y.ToEnumerable<TElement>());
    }
}


