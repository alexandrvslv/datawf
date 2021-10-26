using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
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

        public virtual int Compare(object x, object y)
        {
            for (int i = 0; i < Count; i++)
            {
                int retval = this[i].Compare(x, y);
                if (retval != 0)
                    return retval;
            }
            if (x is IComparable compareable)
                return compareable.CompareTo(y);
            return x?.GetHashCode().CompareTo(y?.GetHashCode() ?? 0) ?? 0;
        }

        public override bool Equals(object obj)
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
            return object.ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class InvokerComparerList<T> : List<IComparer<T>>, IComparerList<T>, IComparer
    {
        public InvokerComparerList()
        { }

        public InvokerComparerList(IEnumerable<IComparer<T>> comparers)
            : base(comparers)
        { }

        public virtual int Compare(T x, T y)
        {
            for (int i = 0; i < Count; i++)
            {
                int retval = this[i].Compare(x, y);
                if (retval != 0)
                    return retval;
            }
            if (x is IComparable<T> genCompareable)
                return genCompareable.CompareTo(y);
            else if (x is IComparable compareable)
                return compareable.CompareTo(y);
            return x?.GetHashCode().CompareTo(y?.GetHashCode() ?? 0) ?? 0;
        }

        public int Compare(object x, object y)
        {
            return Compare((T)x, (T)y);
        }

        public override bool Equals(object obj)
        {
            if (obj is InvokerComparerList<T> list)
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
            return object.ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

}
