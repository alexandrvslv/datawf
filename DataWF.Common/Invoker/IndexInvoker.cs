using System;

namespace DataWF.Common
{
    public class IndexInvoker<T, V, K> : IIndexInvoker<T, V, K>
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

        public V Get(T target, K index)
        {
            return GetAction(target, index);
        }

        public V Get(T target)
        {
            return Get(target, Index);
        }

        public object Get(object target)
        {
            return Get((T)target, Index);
        }

        public object Get(object target, object index)
        {
            return Get((T)target, (K)index);
        }

        public void Set(T target, K index, V value)
        {
            SetAction(target, index, value);
        }

        public void Set(T target, V value)
        {
            Set(target, Index, value);
        }

        public void Set(object target, object value)
        {
            Set((T)target, Index, (V)value);
        }

        public void Set(object target, object index, object value)
        {
            Set((T)target, (K)index, (V)value);
        }

        public IListIndex CreateIndex()
        {
            return new ListIndex<T, V>(this);
        }
    }
}
