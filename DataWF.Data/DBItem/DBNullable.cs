using System;


namespace DataWF.Data
{
    public struct DBNullable<T> : IComparable, IComparable<DBNullable<T>>, IEquatable<DBNullable<T>>//where T : struct,
    {
        private bool notNull;
        private T value;

        public DBNullable(T item)
        {
            this.value = item;
            notNull = true;
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
                notNull = true;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj is DBNullable<T>
                          ? (DBNullable<T>)obj
                          : obj is T
                          ? (DBNullable<T>)(T)obj
                          : new DBNullable<T>());
        }

        public bool Equals(DBNullable<T> other)
        {
            return notNull.Equals(other.notNull) && value.Equals(other.value);
        }

        public override int GetHashCode()
        {
            return notNull.GetHashCode() ^ value.GetHashCode();
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
            if (result == 0)
                result = ((IComparable<T>)value).CompareTo(other.value);
            return result;
        }

        public override string ToString()
        {
            return notNull ? value.ToString() : string.Empty;
        }
    }
}
