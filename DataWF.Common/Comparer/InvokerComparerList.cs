using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace DataWF.Common
{
    /// <summary>
    /// Comparer list. for compound sorting and grouping
    /// </summary>
    public class InvokerComparerList : IComparerList
    {
        protected List<IComparer> comparers;

        public List<IComparer> Comparers
        {
            get { return comparers; }
        }

        public InvokerComparerList() : this(new List<IComparer>())
        {
        }

        public InvokerComparerList(List<IComparer> comparers)
        {
            this.comparers = comparers;
        }

        public void Add(IComparer comparer)
        {
            comparers.Add(comparer);
        }

        public virtual int Compare(object x, object y)
        {
            if ((x == null && y == null) || (x != null && x.Equals(y)))
                return 0;
            for (int i = 0; i < comparers.Count; i++)
            {
                int retval = comparers[i].Compare(x, y);
                if (retval != 0)
                    return retval;
            }
            return x?.GetHashCode().CompareTo(y.GetHashCode()) ?? 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is InvokerComparerList)
            {
                var list = (InvokerComparerList)obj;
                if (list.comparers.Count == comparers.Count)
                {
                    for (int i = 0; i < comparers.Count; i++)
                    {
                        if (!list.comparers[i].Equals(comparers[i]))
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

    public class InvokerComparerList<T> : IComparerList<T>
    {
        protected List<IComparer<T>> comparers;

        public InvokerComparerList() : this(new List<IComparer<T>>())
        {
        }

        public InvokerComparerList(List<IComparer<T>> comparers)
        {
            this.comparers = comparers;
        }

        public List<IComparer<T>> Comparers
        {
            get { return comparers; }
        }

        public void Add(IComparer<T> comparer)
        {
            comparers.Add(comparer);
        }

        public virtual int Compare(T x, T y)
        {
            for (int i = 0; i < comparers.Count; i++)
            {
                int retval = comparers[i].Compare(x, y);
                if (retval != 0)
                    return retval;
            }
            return 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is InvokerComparerList<T>)
            {
                var list = (InvokerComparerList<T>)obj;
                if (list.comparers.Count == comparers.Count)
                {
                    for (int i = 0; i < comparers.Count; i++)
                    {
                        if (!list.comparers[i].Equals(comparers[i]))
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
