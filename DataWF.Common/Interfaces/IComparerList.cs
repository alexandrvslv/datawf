using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface IComparerList : IComparer
    {
        void Add(IComparer comparer);
    }

    public interface IComparerList<T> : IComparer<T>
    {
        void Add(IComparer<T> comparer);
    }

}

