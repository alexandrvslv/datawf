using System;

namespace DataWF.Common
{
    public abstract class Invoker<T, V> : IInvoker<T, V>
    {
        public string Name { get; set; }

        public Type DataType { get { return typeof(V); } }

        public Type TargetType { get { return typeof(T); } }

        public abstract bool CanWrite { get; }

        public IListIndex CreateIndex()
        {
            return ListIndexFabric.Create<T, V>(this);
        }

        public QueryParameter<T> CreateParameter()
        {
            return new QueryParameter<T> { Invoker = this };
        }

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

    }
}
