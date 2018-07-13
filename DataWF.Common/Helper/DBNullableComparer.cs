using System;
using System.Collections.Generic;

namespace DataWF.Common
{
    public static class DBNullableComparer
    {
        public static readonly DBNullableComparer<string> StringOrdinal = new DBNullableComparer<string>(StringComparer.Ordinal);
        public static readonly DBNullableComparer<string> StringOrdinalIgnoreCase = new DBNullableComparer<string>(StringComparer.OrdinalIgnoreCase);
        public static readonly DBNullableComparer<string> InvariantCulture = new DBNullableComparer<string>(StringComparer.InvariantCulture);
        public static readonly DBNullableComparer<string> InvariantCultureIgnoreCase = new DBNullableComparer<string>(StringComparer.InvariantCultureIgnoreCase);
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
            return x.NotNull.Equals(y.NotNull) && (x.NotNull ? Comparer.Equals(x.Value, y.Value) : true);
        }

        public int GetHashCode(DBNullable<T> obj)
        {
            return obj.NotNull.GetHashCode() ^ (obj.NotNull ? obj.Value.GetHashCode() : 0);
        }
    }
}
