using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace DataWF.Common
{
    public abstract class InvokerComparer<T> : InvokerComparer, IComparer<T>, IEqualityComparer<T>
    {
        public InvokerComparer()
        { }

        public InvokerComparer(PropertyInfo info, ListSortDirection direction = ListSortDirection.Ascending)
            : base(info, null, direction)
        {
        }

        public InvokerComparer(IInvoker accessor, ListSortDirection direction = ListSortDirection.Ascending)
            : base(accessor, direction)
        {
        }

        public InvokerComparer(string property, ListSortDirection direction = ListSortDirection.Ascending)
            : base(typeof(T), property, direction)
        {
        }

        public abstract int CompareVal(T x, object key);

        public abstract int Compare(T x, T y);

        public abstract bool Equals(T x, T y);

        public abstract int GetHashCode(T obj);

        public void Format(StringBuilder builder)
        {
            builder.Append(ToString());
        }

        public override string ToString()
        {
            return $" {Invoker.Name} {(Direction == ListSortDirection.Ascending ? "ASC" : "DESC")} ";
        }
    }
}
