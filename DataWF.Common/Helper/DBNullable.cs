using System;


namespace DataWF.Common
{
    public struct DBNullable<T> : IComparable, IComparable<DBNullable<T>>, IEquatable<DBNullable<T>>//where T : struct,
    {
        public static readonly DBNullable<T> NullKey = new DBNullable<T>();

        private bool notNull;
        private T value;

        public static DBNullable<T> CheckNull(object value)
        {
            return value != null ? (DBNullable<T>)(T)value : NullKey;
        }

        public DBNullable(T item)
        {
            this.value = item;
            notNull = item != null;
        }

        public static implicit operator T(DBNullable<T> item)
        {
            return item.Value;
        }

        public static implicit operator DBNullable<T>(T item)
        {
            return new DBNullable<T>(item);
        }

        public bool NotNull
        {
            get { return notNull; }
            set
            {
                notNull = value;
                if (!notNull)
                    this.value = default(T);
            }
        }

        public T Value
        {
            get { return value; }
            set
            {
                this.value = value;
                notNull = value != null;
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj is DBNullable<T>
                          ? (DBNullable<T>)obj
                          : obj is T
                          ? (DBNullable<T>)(T)obj
                          : new DBNullable<T>();
            return notNull.Equals(other.notNull) && (notNull ? value.Equals(other.value) : true);
        }

        public bool Equals(DBNullable<T> other)
        {
            return notNull.Equals(other.notNull) && (notNull ? value.Equals(other.value) : true);
        }

        public override int GetHashCode()
        {
            return notNull.GetHashCode() ^ (notNull ? value.GetHashCode() : 0);
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj is DBNullable<T>
                             ? (DBNullable<T>)obj
                             : obj is T
                             ? (DBNullable<T>)(T)obj
                             : new DBNullable<T>());
        }

        public int CompareTo(DBNullable<T> other)
        {
            var result = notNull.CompareTo(other.notNull);
            if (result == 0 && notNull)
                result = ((IComparable<T>)value).CompareTo(other.value);
            return result;
        }

        public override string ToString()
        {
            return notNull ? value.ToString() : string.Empty;
        }
    }
}
