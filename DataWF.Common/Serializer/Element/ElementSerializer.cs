using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DataWF.Common
{
    public abstract class ElementSerializer : IElementSerializer
    {
        public int SizeOfType { get; protected set; }
        public abstract bool CanConvertString { get; }
        #region Binary
        public abstract object ReadObject(BinaryReader reader);

        public abstract void WriteObject(BinaryWriter writer, object value, bool writeToken);

        public abstract object ReadObject(SpanReader reader);

        public abstract void WriteObject(SpanWriter writer, object value, bool writeToken);

        public virtual void WriteObject(BinaryInvokerWriter writer, object value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            WriteObject(writer.Writer, value, true);
        }

        public virtual object ReadObject(BinaryInvokerReader reader, object value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            return ReadObject(reader.Reader);
        }

        #endregion

        #region Xml
        public abstract object ObjectFromString(string value);

        public abstract string ObjectToString(object value);

        public virtual void WriteObject(XmlInvokerWriter writer, object value, TypeSerializeInfo info)
        {
            writer.Writer.WriteValue(ObjectToString(value));
        }

        public virtual object ReadObject(XmlInvokerReader reader, object value, TypeSerializeInfo info)
        {
            return ObjectFromString(reader.Reader.Value);
        }

        #endregion

        #region Json
        public virtual void WriteObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }

        public virtual object ReadObject(ref Utf8JsonReader reader, object value, JsonSerializerOptions info)
        {
            return ObjectFromString(reader.GetString());
        }
        #endregion
    }
}
