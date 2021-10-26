using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DataWF.Common
{
    public abstract class IndexInvoker<T, V, K> : Invoker<T, V>, IIndexInvoker<T, V, K>, IValuedInvoker<V>
    {

        public K Index { get; set; }

        object IIndexInvoker.Index
        {
            get { return Index; }
            set { Index = (K)value; }
        }

        public abstract V GetValue(T target, K index);

        public override V GetValue(T target)
        {
            return GetValue(target, Index);
        }

        public override object GetValue(object target)
        {
            return GetValue((T)target, Index);
        }

        public object GetValue(object target, object index)
        {
            return GetValue(target == null ? default(T) : (T)target, (K)index);
        }

        public abstract void SetValue(T target, K index, V value);

        public override void SetValue(T target, V value)
        {
            SetValue(target, Index, value);
        }

        public override void SetValue(object target, object value)
        {
            SetValue((T)target, Index, (V)value);
        }

        public void SetValue(object target, object index, object value)
        {
            SetValue((T)target, (K)index, (V)value);
        }
    }
}
