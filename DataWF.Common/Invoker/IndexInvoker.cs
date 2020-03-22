using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DataWF.Common
{
    public class IndexInvoker<T, V, K> : IIndexInvoker<T, V, K>, IValuedInvoker<V>
    {
        public IndexInvoker(string name, Func<T, K, V> getAction, Action<T, K, V> setAction = null)
        {
            GetAction = getAction;
            SetAction = setAction;
            DataType = typeof(V);
            Name = name;
        }

        public bool CanWrite { get { return SetAction != null; } }

        public K Index { get; set; }

        object IIndexInvoker.Index
        {
            get { return Index; }
            set { Index = (K)value; }
        }

        public string Name { get; set; }

        public Type DataType { get; set; }

        public Type TargetType { get { return typeof(T); } }

        internal Func<T, K, V> GetAction { get; private set; }

        internal Action<T, K, V> SetAction { get; private set; }

        public V GetValue(T target, K index)
        {
            return GetAction(target, index);
        }

        public V GetValue(T target)
        {
            return GetValue(target, Index);
        }

        public object GetValue(object target)
        {
            return GetValue((T)target, Index);
        }

        public object GetValue(object target, object index)
        {
            return GetValue(target == null ? default(T) : (T)target, (K)index);
        }

        public void SetValue(T target, K index, V value)
        {
            SetAction(target, index, value);
        }

        public void SetValue(T target, V value)
        {
            SetValue(target, Index, value);
        }

        public void SetValue(object target, object value)
        {
            SetValue((T)target, Index, (V)value);
        }

        public void SetValue(object target, object index, object value)
        {
            SetValue((T)target, (K)index, (V)value);
        }

        public IListIndex CreateIndex(bool concurrent)
        {
            return ListIndexFabric.Create<T, V>(this, concurrent);
        }

        public IQueryParameter CreateParameter()
        {
            return new QueryParameter<T>(this);
        }

        public InvokerComparer CreateComparer()
        {
            return new InvokerComparer<T, V>(this);
        }

        public bool CheckItem(T item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return ListHelper.CheckItem(GetValue(item), typedValue, comparer, comparision);//(IComparer<V>)
        }

        public bool CheckItem(object item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return CheckItem((T)item, typedValue, comparer, comparision);
        }

        V IValuedInvoker<V>.GetValue(object target)
        {
            return GetValue((T)target);
        }

        public void SetValue(object target, V value)
        {
            SetValue((T)target, value);
        }

    }
}
