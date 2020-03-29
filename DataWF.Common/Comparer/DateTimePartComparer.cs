using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class DateTimePartComparer : IEqualityComparer<DateTime>, IEqualityComparer<DateTime?>, IEqualityComparer, IComparer<DateTime>, IComparer<DateTime?>
    {
        public static readonly DateTimePartComparer Default = new DateTimePartComparer();

        public bool Equals(DateTime x, DateTime y)
        {
            if (x.TimeOfDay == TimeSpan.Zero || y.TimeOfDay == TimeSpan.Zero)
                return x.Date.Equals(y.Date);
            return x.Equals(y);
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            return Equals((DateTime)x, (DateTime)y);
        }

        public int GetHashCode(DateTime obj)
        {
            return obj.GetHashCode();
        }

        public int GetHashCode(object obj)
        {
            return obj.GetHashCode();
        }

        public int Compare(DateTime x, DateTime y)
        {
            if (x.TimeOfDay == TimeSpan.Zero || y.TimeOfDay == TimeSpan.Zero)
                return x.Date.CompareTo(y.Date);
            return x.CompareTo(y);
        }

        public bool Equals(DateTime? x, DateTime? y)
        {
            return Equals(x ?? DateTime.MinValue, y ?? DateTime.MinValue);
        }

        public int GetHashCode(DateTime? obj)
        {
            return obj.GetHashCode();
        }

        public int Compare(DateTime? x, DateTime? y)
        {
            return Compare(x ?? DateTime.MinValue, y ?? DateTime.MinValue);
        }
    }
}

