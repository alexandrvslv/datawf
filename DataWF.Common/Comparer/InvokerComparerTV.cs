﻿using System.ComponentModel;
using System.Reflection;

namespace DataWF.Common
{
    public class InvokerComparer<T, V> : InvokerComparer<T>
    {
        private IInvoker<T, V> valuedInvoker;

        public InvokerComparer()
        { }

        public InvokerComparer(PropertyInfo info, ListSortDirection direction = ListSortDirection.Ascending)
            : base(info, direction)
        {
        }

        public InvokerComparer(IInvoker<T, V> accessor, ListSortDirection direction = ListSortDirection.Ascending)
            : base(accessor, direction)
        {
        }

        public InvokerComparer(string property, ListSortDirection direction = ListSortDirection.Ascending)
            : base(property, direction)
        {
        }

        public override IInvoker Invoker
        {
            get => ValueInvoker;
            set
            {
                valuedInvoker = (IInvoker<T, V>)value;
                Name = valuedInvoker.Name;
            }
        }

        public IInvoker<T, V> ValueInvoker
        {
            get => valuedInvoker ?? (valuedInvoker = (IInvoker<T, V>)EmitInvoker.Initialize<T>(Name));
        }

        public override int CompareVal(object x, object key)
        {
            return CompareVal((T)x, (V)key);
        }

        public override int CompareVal(T x, object key)
        {
            return CompareVal(x, (V)key);
        }

        public virtual int CompareVal(T x, V key)
        {
            var val = ValueInvoker.GetValue(x);
            var result = ListHelper.Compare(val, key, null, false);
            return Direction == ListSortDirection.Ascending ? result : -result;
        }

        public override int Compare(object x, object y)
        {
            return Compare((T)x, (T)y);
        }

        public override int Compare(T x, T y)
        {
            var xValue = x == null ? default(V) : ValueInvoker.GetValue(x);
            var yValue = y == null ? default(V) : ValueInvoker.GetValue(y);
            var result = ListHelper.Compare<V>(xValue, yValue, null, false);
            return Direction == ListSortDirection.Ascending ? result : -result;
        }

        public override bool Equals(object x, object y)
        {
            return Equals((T)x, (T)y);
        }

        public override bool Equals(T x, T y)
        {
            var xValue = x == null ? default(V) : ValueInvoker.GetValue(x);
            var yValue = y == null ? default(V) : ValueInvoker.GetValue(y);
            return ListHelper.Equal(xValue, yValue);
        }

        public override int GetHashCode(object obj)
        {
            return GetHashCode((T)obj);
        }

        public override int GetHashCode(T obj)
        {
            var objValue = obj == null ? default(V) : ValueInvoker.GetValue(obj);
            return objValue?.GetHashCode() ?? obj.GetHashCode();
        }
    }
}