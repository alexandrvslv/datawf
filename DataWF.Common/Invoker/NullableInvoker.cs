using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Common
{
    public abstract class NullableInvoker<T, V> : Invoker<T, V?> where V : struct
    {
        public override InvokerComparer CreateComparer(Type type, ListSortDirection direction = ListSortDirection.Ascending)
        {
            type = type ?? typeof(T);
            return (InvokerComparer)Activator.CreateInstance(typeof(NullableInvokerComparer<,>).MakeGenericType(type, typeof(V)), (IInvoker)this, direction);
        }

        public override InvokerComparer<TT> CreateComparer<TT>(ListSortDirection direction = ListSortDirection.Ascending)
        {
            return new NullableInvokerComparer<TT, V>(this, direction);
        }

        public override bool CheckItem(T item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return ListHelper.CheckItemN<V>(GetValue(item), typedValue, comparer, (IComparer<V?>)comparision);//
        }
    }
}
