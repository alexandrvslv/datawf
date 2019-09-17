using System;
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

        public IListIndex CreateIndex(bool concurrent)
        {
            return ListIndexFabric.Create<T, V>(this, concurrent);
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
