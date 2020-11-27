using System;
using System.Reflection;

namespace DataWF.Common
{
    public interface IPropertySerializeInfo : INamed
    {
        Type DataType { get; set; }
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
        ElementSerializer Serializer { get; }
        bool CheckDefault(object value);

        void PropertyToBinary(BinaryInvokerWriter writer, object element);
        void PropertyToBinary<E>(BinaryInvokerWriter writer, E element);

        void PropertyFromBinary(BinaryInvokerReader reader, object element, TypeSerializeInfo typeInfo);
        void PropertyFromBinary<E>(BinaryInvokerReader reader, E element, TypeSerializeInfo typeInfo);

        void PropertyToString(XmlInvokerWriter writer, object element);
        void PropertyToString<E>(XmlInvokerWriter writer, E element);

        void PropertyFromString(XmlInvokerReader reader, object element, TypeSerializeInfo typeInfo);
        void PropertyFromString<E>(XmlInvokerReader reader, E element, TypeSerializeInfo typeInfo);
    }

    public interface IPropertySerializationInfo<T> : IPropertySerializeInfo

    {
    }
}