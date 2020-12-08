using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Common
{
    public class SequenceComparer<T> : IComparer<IEnumerable<T>>, IEqualityComparer<IEnumerable<T>>
    {
        public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
        {
            if (x == y)
                return true;
            if (x == null || y == null)
            {
                return false;
            }
            return x.SequenceEqual(y);
        }

        public int GetHashCode(IEnumerable<T> obj)
        {
            return obj.GetHashCode();
        }

        public int Compare(IEnumerable<T> x, IEnumerable<T> y)
        {
            return SequenceCompare(x, y);
        }

        public static int SequenceCompare(IEnumerable<T> xEnumerable, IEnumerable<T> yEnumerable)
        {
            if (xEnumerable == yEnumerable)
                return 0;
            if (xEnumerable == null)
            {
                return -1;
            }
            if (yEnumerable == null)
            {
                return 1;
            }
            int result = 0;
            var xEnumer = xEnumerable.GetEnumerator();
            var yEnumer = yEnumerable.GetEnumerator();
            var comparer = ListHelperComparer<T>.Default;
            while (true)
            {
                var xExist = xEnumer.MoveNext();
                var yExist = yEnumer.MoveNext();
                if (!xExist)
                {
                    result = yExist ? -1 : 0;
                    break;
                }
                if (!yExist)
                {
                    result = 1;
                    break;
                }
                result = comparer.Compare(xEnumer.Current, yEnumer.Current);
                if (result != 0)
                    break;
            }

            return result;
        }
    }

}

