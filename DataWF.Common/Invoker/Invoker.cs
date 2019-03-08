using System;

namespace DataWF.Common
{
    public class Invoker<T, V> : IInvoker<T, V>
    {
        public Invoker(string name, Func<T, V> getAction, Action<T, V> setAction = null)
        {
            GetAction = getAction;
            SetAction = setAction;
            Name = name;
        }

        public bool CanWrite { get { return SetAction != null; } }

        public string Name { get; set; }

        public Type DataType { get { return typeof(V); } }

        public Type TargetType { get { return typeof(T); } }

        public Func<T, V> GetAction { get; protected set; }

        public Action<T, V> SetAction { get; protected set; }

        public IListIndex CreateIndex()
        {
            return ListIndexFabric.Create<T, V>(this);
        }

        public QueryParameter<T> CreateParameter()
        {
            return new QueryParameter<T> { Invoker = this };
        }

        public V GetValue(T target)
        {
            return GetAction(target);
        }

        public object GetValue(object target)
        {
            //Debug.WriteLineIf(target != null && !typeof(T).IsInstanceOfType(target), $"expected {typeof(T)} but get {target.GetType()}");
            return GetValue((T)target);
        }

        public void SetValue(T target, V value)
        {
            SetAction(target, value);
        }

        public void SetValue(object target, object value)
        {
            SetValue((T)target, (V)value);
        }

        public override string ToString()
        {
            return $"{typeof(T).Name}.{Name} {typeof(V).Name}";
        }
    }
}
