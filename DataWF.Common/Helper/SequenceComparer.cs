using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Common
{
    public class SequenceComparer : IComparer<IEnumerable>, IEqualityComparer<IEnumerable>
    {
        public bool Equals(IEnumerable x, IEnumerable y)
        {
            if (x == y)
                return true;
            if (x == null || y == null)
            {
                return false;
            }
            return x.ToEnumerable().SequenceEqual(y.ToEnumerable());
        }

        public int GetHashCode(IEnumerable obj)
        {
            return obj.GetHashCode();
        }

        public int Compare(IEnumerable x, IEnumerable y)
        {
            return SequenceCompare(x, y);
        }

        public static int SequenceCompare(IEnumerable xEnumerable, IEnumerable yEnumerable)
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
                result = ListHelper.Compare(xEnumer.Current, yEnumer.Current, (IComparer)null);
                if (result != 0)
                    break;
            }

            return result;
        }
    }

}

