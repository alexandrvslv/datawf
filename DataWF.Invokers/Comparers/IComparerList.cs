using System.Collections;
using System.Collections.Generic;

namespace DataWF.Comparers;

public interface IComparerList : IComparer
{
    void Add(IComparer comparer);
}

public interface IComparerList<T> : IComparer<T>, IEnumerable<UniversalComparer<T>>
{
    int Count { get; }
    UniversalComparer<T> this[int index] { get; set; }
    void Add(UniversalComparer<T> comparer);
}


