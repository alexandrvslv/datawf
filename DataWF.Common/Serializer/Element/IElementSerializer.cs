using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public interface IElementSerializer
    {
        bool CanConvertString { get; }

        #region Binary
        object ConvertFromBinary(BinaryReader reader);
        void ConvertToBinary(BinaryWriter writer, object value, bool writeToken);

        void PropertyToBinary(BinaryInvokerWriter writer, object element, IInvoker invoker);
        void PropertyToBinary<E>(BinaryInvokerWriter writer, E element, IInvoker invoker);

        void PropertyFromBinary(BinaryInvokerReader reader, object element, IInvoker invoker);
        void PropertyFromBinary<E>(BinaryInvokerReader reader, E element, IInvoker invoker);

        void Write(BinaryInvokerWriter writer, object value, TypeSerializationInfo info, Dictionary<ushort, IPropertySerializationInfo> map);
        object Read(BinaryInvokerReader reader, object value, TypeSerializationInfo info, Dictionary<ushort, IPropertySerializationInfo> map);
        #endregion

        #region Xml
        object ConvertFromString(string value);
        string ConvertToString(object value);

        void PropertyToString(XmlInvokerWriter writer, object element, IPropertySerializationInfo property);
        void PropertyToString<E>(XmlInvokerWriter writer, E element, IPropertySerializationInfo property);

        void PropertyFromString(XmlInvokerReader reader, object element, IPropertySerializationInfo property, TypeSerializationInfo typeInfo);
        void PropertyFromString<E>(XmlInvokerReader reader, E element, IPropertySerializationInfo property, TypeSerializationInfo typeInfo);

        void Write(XmlInvokerWriter writer, object value, TypeSerializationInfo info);
        object Read(XmlInvokerReader reader, object value, TypeSerializationInfo info);
        #endregion
    }
}