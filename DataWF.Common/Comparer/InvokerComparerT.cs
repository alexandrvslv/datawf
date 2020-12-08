using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace DataWF.Common
{
    public class InvokerComparer<T> : InvokerComparer, IComparer<T>, IEqualityComparer<T>
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

        public override IInvoker Invoker
        {
            get => base.Invoker ?? (base.Invoker = EmitInvoker.Initialize<T>(Name));
            set => base.Invoker = value;
        }

        public virtual int CompareVal(T x, object key)
        {
            return base.CompareVal(x, key);
        }

        public virtual int Compare(T x, T y)
        {
            return base.Compare(x, y);
        }

        public virtual bool Equals(T x, T y)
        {
            return base.Equals(x, y);
        }

        public virtual int GetHashCode(T obj)
        {
            return base.GetHashCode(obj);
        }

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
