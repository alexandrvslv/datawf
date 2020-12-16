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

    public abstract class Invoker<T, V> : IInvoker<T, V>, IValuedInvoker<V>, IInvokerExtension
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

        public virtual IListIndex CreateIndex(bool concurrent)
        {
            return ListIndexFactory.Create<T, V>(this, concurrent);
        }

        public virtual IListIndex CreateIndex<TT>(bool concurrent)
        {
            return ListIndexFactory.Create<TT, V>(this, concurrent);
        }

        public virtual IQueryParameter CreateParameter(Type type)
        {
            type = type ?? typeof(T);
            return (IQueryParameter)Activator.CreateInstance(typeof(QueryParameter<,>).MakeGenericType(type, typeof(V)), (IInvoker)this);
        }

        public virtual IQueryParameter CreateParameter(Type type, CompareType compare, object value)
        {
            type = type ?? typeof(T);
            var parameter = CreateParameter(type);
            parameter.Comparer = compare;
            parameter.Value = value;
            return parameter;
        }

        public virtual IQueryParameter CreateParameter(Type type, LogicType logic, CompareType compare, object value = null, QueryGroup group = QueryGroup.None)
        {
            type = type ?? typeof(T);
            var parameter = CreateParameter(type);
            parameter.Logic = logic;
            parameter.Comparer = compare;
            parameter.Value = value;
            parameter.Group = group;
            return parameter;
        }

        public virtual IQueryParameter<TT> CreateParameter<TT>()
        {
            return CreateParameter<TT>(CompareType.Equal, null);
        }

        public virtual IQueryParameter<TT> CreateParameter<TT>(CompareType compare, object value)
        {
            return new QueryParameter<TT, V> { Invoker = (IInvoker<TT, V>)this, Comparer = compare, Value = value };
        }

        public virtual IQueryParameter<TT> CreateParameter<TT>(LogicType logic, CompareType compare, object value = null, QueryGroup group = QueryGroup.None)
        {
            return new QueryParameter<TT, V> { Logic = logic, Invoker = (IInvoker<TT, V>)this, Comparer = compare, Value = value, Group = group };
        }

        public virtual IComparer CreateComparer(Type type, ListSortDirection direction = ListSortDirection.Ascending)
        {
            type = type ?? typeof(T);
            return (InvokerComparer)Activator.CreateInstance(typeof(InvokerComparer<,>).MakeGenericType(type, typeof(V)), (IInvoker)this, direction);
        }

        public virtual IComparer<TT> CreateComparer<TT>(ListSortDirection direction = ListSortDirection.Ascending)
        {
            return new InvokerComparer<TT, V>((IInvoker<TT, V>)this, direction);
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
