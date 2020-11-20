using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DataWF.Common
{
    public abstract class ElementSerializer : IElementSerializer
    {
        public abstract bool CanConvertString { get; }
        #region Binary
        public abstract object ConvertFromBinary(BinaryReader reader);

        public abstract void ConvertToBinary(BinaryWriter writer, object value, bool writeToken);

        public virtual void Write(BinaryInvokerWriter writer, object value, TypeSerializationInfo info, Dictionary<ushort, IPropertySerializationInfo> map)
        {
            ConvertToBinary(writer.Writer, value, true);
        }

        public virtual object Read(BinaryInvokerReader reader, object value, TypeSerializationInfo info, Dictionary<ushort, IPropertySerializationInfo> map)
        {
            return ConvertFromBinary(reader.Reader);
        }

        public abstract void PropertyToBinary(BinaryInvokerWriter writer, object element, IInvoker invoker);

        public abstract void PropertyToBinary<E>(BinaryInvokerWriter writer, E element, IInvoker invoker);

        public abstract void PropertyFromBinary(BinaryInvokerReader reader, object element, IInvoker invoker);

        public abstract void PropertyFromBinary<E>(BinaryInvokerReader reader, E element, IInvoker invoker);
        #endregion

        #region Xml
        public abstract object ConvertFromString(string value);

        public abstract string ConvertToString(object value);

        public virtual void Write(XmlInvokerWriter writer, object value, TypeSerializationInfo info)
        {
            writer.Writer.WriteValue(ConvertToString(value));
        }

        public virtual object Read(XmlInvokerReader reader, object value, TypeSerializationInfo info)
        {
            return ConvertFromString(reader.Reader.Value);
        }

        public abstract void PropertyToString(XmlInvokerWriter writer, object element, IPropertySerializationInfo property);

        public abstract void PropertyToString<E>(XmlInvokerWriter writer, E element, IPropertySerializationInfo property);

        public abstract void PropertyFromString(XmlInvokerReader writer, object element, IPropertySerializationInfo property, TypeSerializationInfo itemType);

        public abstract void PropertyFromString<E>(XmlInvokerReader writer, E element, IPropertySerializationInfo property, TypeSerializationInfo itemType);
        #endregion

        #region Json
        public virtual void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }

        public virtual object Read(ref Utf8JsonReader reader, object value, JsonSerializerOptions info)
        {
            return ConvertFromString(reader.GetString());
        }
        #endregion
    }
}
