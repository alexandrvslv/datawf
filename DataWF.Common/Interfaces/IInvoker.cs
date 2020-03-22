using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.ComponentModel;
using System.Text.Json;

namespace DataWF.Common
{
    public interface IInvoker : IValueProvider, INamed
    {
        Type DataType { get; }
        Type TargetType { get; }
        bool CanWrite { get; }

        bool CheckItem(object item, object typedValue, CompareType comparer, IComparer comparision);
    }

    public interface IInvokerExtension
    {
        IListIndex CreateIndex(bool concurrent);

        IQueryParameter CreateParameter(Type type);
        QueryParameter<TT> CreateParameter<TT>();

        InvokerComparer CreateComparer(Type type, ListSortDirection direction = ListSortDirection.Ascending);
        InvokerComparer<TT> CreateComparer<TT>(ListSortDirection direction = ListSortDirection.Ascending);
    }

    public interface IValuedInvoker<V> : IInvoker
    {
        new V GetValue(object target);
        void SetValue(object target, V value);
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

    public interface IInvokerJson<T> : IInvokerJson
    {
        void WriteValue(Utf8JsonWriter writer, T value, JsonSerializerOptions option);
        void ReadValue(ref Utf8JsonReader reader, T value, JsonSerializerOptions option);
    }
}
