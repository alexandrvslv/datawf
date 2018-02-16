using System;
using System.Collections.Generic;

namespace DataWF.Data
{
    public static class DBNullableComparer
    {
        public static DBNullableComparer<string> StringOrdinalIgnoreCase
        {
            get { return new DBNullableComparer<string>(StringComparer.OrdinalIgnoreCase); }
        }
    }

    public class DBNullableComparer<T> : IEqualityComparer<DBNullable<T>>
    {
        public IEqualityComparer<T> Comparer;

        public DBNullableComparer(IEqualityComparer<T> comparer)
        {
            Comparer = comparer;
        }

        public bool Equals(DBNullable<T> x, DBNullable<T> y)
        {
            return x.NotNull.Equals(y.NotNull) && Comparer.Equals(x.Value, y.Value);
        }

        public int GetHashCode(DBNullable<T> obj)
        {
            return obj.NotNull.GetHashCode() ^ Comparer.GetHashCode(obj.Value);
        }
    }
}
