using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Text.Json;

namespace DataWF.Common
{
    public interface IInvoker : IValueProvider, INamed
    {
        Type DataType { get; }
        Type TargetType { get; }
        bool CanWrite { get; }

        IListIndex CreateIndex(bool concurrent);
        IQueryParameter CreateParameter();
        InvokerComparer CreateComparer();

        bool CheckItem(object item, object typedValue, CompareType comparer, IComparer comparision);
    }

    public interface IValuedInvoker : IInvoker
    {
        V GetValue<V>(object target);
        void SetValue<V>(object target, V value);
    }

    public interface IInvoker<T, V> : IInvoker
    {
        V GetValue(T target);
        void SetValue(T target, V value);

        bool CheckItem(T item, object typedValue, CompareType comparer, IComparer comparision);
    }

    //Boxing optimization
    public interface IInvokerJson : IInvoker
    {
        void WriteValue(Utf8JsonWriter writer, object value, JsonSerializerOptions option);
        void ReadValue(ref Utf8JsonReader reader, object value, JsonSerializerOptions option);

    }
}
