using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DataWF.Common
{

    public abstract class Invoker<T, V> : IInvoker<T, V>, IValuedInvoker<V>
    {
        public Invoker()
        {
            DataType = typeof(V);
        }

        public abstract string Name { get; }

        public Type DataType { get; set; }// { get { return typeof(V); } }

        public Type TargetType { get { return typeof(T); } }

        public abstract bool CanWrite { get; }

        string INamed.Name { get => Name; set { } }

        public abstract V GetValue(T target);

        V IValuedInvoker<V>.GetValue(object target)
        {
            return GetValue((T)target);
        }

        public virtual object GetValue(object target) => GetValue((T)target);

        public abstract void SetValue(T target, V value);

        public void SetValue(object target, V value)
        {
            SetValue((T)target, value);
        }

        public virtual void SetValue(object target, object value) => SetValue((T)target, (V)value);

        public override string ToString()
        {
            return $"{typeof(T).Name}.{Name} {typeof(V).Name}";
        }

        public bool CheckItem(object item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return CheckItem((T)item, typedValue, comparer, comparision);
        }

        public virtual bool CheckItem(T item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return ListHelper.CheckItemT<V>(GetValue(item), typedValue, comparer, (IComparer<V>)comparision);//
        }
    }
}
