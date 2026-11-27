using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections;

public interface ISortable : IList
{
    IComparer? Comparer { get; }

    bool Disposed { get; }

    Type ItemType { get; }

    void Sort();

    void ApplySort(IComparer comparer);

    void RemoveSort();

}

public interface ISortable<T> : ISortable, IList<T>
{
    new IComparer<T>? Comparer { get; }
    IComparer? ISortable.Comparer => Comparer as IComparer;   

    void ApplySort(IComparer<T> comparer);
}
