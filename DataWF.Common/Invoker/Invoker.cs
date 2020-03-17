using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DataWF.Common
{

    public abstract class Invoker<T, V> : IInvoker<T, V>, IValuedInvoker, IInvokerJson
    {
        public abstract string Name { get; }

        public Type DataType { get { return typeof(V); } }

        public Type TargetType { get { return typeof(T); } }

        public abstract bool CanWrite { get; }

        string INamed.Name { get => Name; set { } }

        public abstract V GetValue(T target);

        public L GetValue<L>(object target)
        {
            var value = GetValue((T)target);
            return Unsafe.As<V, L>(ref value);
        }

        public object GetValue(object target) => GetValue((T)target);

        public abstract void SetValue(T target, V value);

        public void SetValue<L>(object target, L value)
        {
            var converted = Unsafe.As<L, V>(ref value);
            SetValue((T)target, converted);
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

        public void WriteValue(Utf8JsonWriter writer, object item, JsonSerializerOptions option)
        {
            var value = GetValue((T)item);
            JsonSerializer.Serialize<V>(writer, value, option);
        }

        public void ReadValue(ref Utf8JsonReader reader, object item, JsonSerializerOptions option)
        {
            var value = JsonSerializer.Deserialize<V>(ref reader, option);
            SetValue((T)item, value);
        }
    }
}
