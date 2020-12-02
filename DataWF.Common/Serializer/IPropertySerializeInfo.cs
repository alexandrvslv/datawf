using System;
using System.Reflection;
using System.Text.Json;

namespace DataWF.Common
{
    public interface IPropertySerializeInfo : INamed
    {
        Type DataType { get; }
        object Default { get; }
        IInvoker PropertyInvoker { get; }
        bool IsAttribute { get; }
        bool IsChangeSensitive { get; }
        bool IsReadOnly { get; }
        bool IsRequired { get; }
        bool IsText { get; }
        bool IsWriteable { get; }
        int Order { get; set; }
        PropertyInfo PropertyInfo { get; }
        IElementSerializer Serializer { get; }

        bool CheckDefault(object value);

        void Write(BinaryInvokerWriter writer, object element);
        void Write<E>(BinaryInvokerWriter writer, E element);

        void Read(BinaryInvokerReader reader, object element, TypeSerializeInfo typeInfo);
        void Read<E>(BinaryInvokerReader reader, E element, TypeSerializeInfo typeInfo);

        void Write(XmlInvokerWriter writer, object element);
        void Write<E>(XmlInvokerWriter writer, E element);

        void Read(XmlInvokerReader reader, object element, TypeSerializeInfo typeInfo);
        void Read<E>(XmlInvokerReader reader, E element, TypeSerializeInfo typeInfo);

        void Write(Utf8JsonWriter writer, object element, JsonSerializerOptions options = null);

        void Write<E>(Utf8JsonWriter writer, E element, JsonSerializerOptions options = null);

        void Read(ref Utf8JsonReader reader, object element, JsonSerializerOptions options = null);

        void Read<E>(ref Utf8JsonReader reader, E element, JsonSerializerOptions options = null);
    }

    public interface IPropertySerializationInfo<T> : IPropertySerializeInfo

    {
    }
}