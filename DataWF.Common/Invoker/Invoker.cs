using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;

namespace DataWF.Common
{

    public abstract class Invoker<T, V> : IInvoker<T, V>
    {
        public abstract string Name { get; }

        public Type DataType { get { return typeof(V); } }

        public Type TargetType { get { return typeof(T); } }

        public abstract bool CanWrite { get; }

        string INamed.Name { get => Name; set { } }

        public abstract V GetValue(T target);

        public object GetValue(object target)
        {
            return GetValue((T)target);
        }

        public abstract void SetValue(T target, V value);

        public void SetValue(object target, object value)
        {
            SetValue((T)target, (V)value);
        }

        public override string ToString()
        {
            return $"{typeof(T).Name}.{Name} {typeof(V).Name}";
        }

        public virtual IListIndex CreateIndex(bool concurrent)
        {
            return ListIndexFabric.Create<T, V>(this, concurrent);
        }

        IQueryParameter IInvoker.CreateParameter()
        {
            return CreateParameter();
        }

        public virtual QueryParameter<T> CreateParameter()
        {
            return new QueryParameter<T> { Invoker = this };
        }

        InvokerComparer IInvoker.CreateComparer()
        {
            return CreateComparer();
        }

        public virtual InvokerComparer<T, V> CreateComparer()
        {
            return new InvokerComparer<T, V>(this);
        }

        public bool CheckItem(object item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return CheckItem((T)item, typedValue, comparer, comparision);
        }

        public bool CheckItem(T item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return ListHelper.CheckItem(GetValue(item), typedValue, comparer, comparision);//(IComparer<V>)
        }
    }
}
