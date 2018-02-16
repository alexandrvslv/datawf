using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface ISortable
    {
        object NewItem();

        Type ItemType { get; }

        void Sort();

        void ApplySort(IComparer comparer);

        void RemoveSort();
    }

    public interface ISortable<T> : ISortable
    {
        void ApplySort(IComparer<T> comparer);
    }
}
