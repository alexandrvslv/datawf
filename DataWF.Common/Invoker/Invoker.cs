﻿using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DataWF.Common
{

    public abstract class Invoker<T, V> : IInvoker<T, V>, IValuedInvoker<V>, IInvokerJson, IInvokerExtension
    {
        public abstract string Name { get; }

        public Type DataType { get { return typeof(V); } }

        public Type TargetType { get { return typeof(T); } }

        public abstract bool CanWrite { get; }

        string INamed.Name { get => Name; set { } }

        public abstract V GetValue(T target);

        V IValuedInvoker<V>.GetValue(object target)
        {
            return GetValue((T)target);
        }

        public object GetValue(object target) => GetValue((T)target);

        public abstract void SetValue(T target, V value);

        public void SetValue(object target, V value)
        {
            SetValue((T)target, value);
        }

        public void SetValue(object target, object value) => SetValue((T)target, (V)value);

        public override string ToString()
        {
            return $"{typeof(T).Name}.{Name} {typeof(V).Name}";
        }

        public virtual IListIndex CreateIndex(bool concurrent)
        {
            return ListIndexFabric.Create<T, V>(this, concurrent);
        }

        public virtual IQueryParameter CreateParameter(Type type)
        {
            type = type ?? typeof(T);
            return (IQueryParameter)Activator.CreateInstance(typeof(QueryParameter<>).MakeGenericType(type), (IInvoker)this);
        }

        public virtual QueryParameter<TT> CreateParameter<TT>()
        {
            return new QueryParameter<TT> { Invoker = this };
        }

        public virtual InvokerComparer CreateComparer(Type type)
        {
            type = type ?? typeof(T);
            return (InvokerComparer)Activator.CreateInstance(typeof(InvokerComparer<,>).MakeGenericType(type, typeof(V)), (IInvoker)this, ListSortDirection.Ascending);
        }

        public virtual InvokerComparer<TT> CreateComparer<TT>()
        {
            return new InvokerComparer<TT, V>(this);
        }

        public bool CheckItem(object item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return CheckItem((T)item, typedValue, comparer, comparision);
        }

        public virtual bool CheckItem(T item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return ListHelper.CheckItemT<V>(GetValue(item), typedValue, comparer, (IComparer<V>)comparision);//
        }

        public virtual void WriteValue(Utf8JsonWriter writer, object item, JsonSerializerOptions option)
        {
            var value = GetValue((T)item);
            JsonSerializer.Serialize<V>(writer, value, option);
        }

        public virtual void ReadValue(ref Utf8JsonReader reader, object item, JsonSerializerOptions option)
        {
            var value = JsonSerializer.Deserialize<V>(ref reader, option);
            SetValue((T)item, value);
        }
    }

    public abstract class NullableInvoker<T, V> : Invoker<T, V?> where V : struct
    {
        public override InvokerComparer CreateComparer(Type type)
        {
            type = type ?? typeof(T);
            return (InvokerComparer)Activator.CreateInstance(typeof(NullableInvokerComparer<,>).MakeGenericType(type, typeof(V)), (IInvoker)this, ListSortDirection.Ascending);
        }

        public override InvokerComparer<TT> CreateComparer<TT>()
        {
            return new NullableInvokerComparer<TT, V>(this);
        }

        public override bool CheckItem(T item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return ListHelper.CheckItemN<V>(GetValue(item), typedValue, comparer, (IComparer<V?>)comparision);//
        }
    }
}
