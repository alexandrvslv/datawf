using System.Collections;
using System.Collections.Generic;

namespace DataWF.Comparers;

public interface IUniversalComparer : IComparer, IEqualityComparer
{ }

public interface IUniversalComparer<in T> : IUniversalComparer, IComparer<T>, IEqualityComparer<T>, IEqualityComparer
    where T: allows ref struct
{
    public bool Equals(T? x, object? y);
}
