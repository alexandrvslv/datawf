using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface ISortable : IList
    {
        bool Disposed { get; }

        object NewItem();

        Type ItemType { get; }

        void Sort();

        void ApplySort(IComparer comparer);

        void RemoveSort();

        IFilterable DefaultView { get; }
    }

    public interface ISortable<T> : ISortable
    {
        void ApplySort(IComparer<T> comparer);
    }
}
