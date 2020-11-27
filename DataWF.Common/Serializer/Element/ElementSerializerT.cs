using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DataWF.Common
{
    public abstract class ElementSerializer<T> : ElementSerializer, IElementSerializer<T>
    {
        #region Binary
        public override object ReadObject(BinaryReader reader) => Read(reader);

        public override void WriteObject(BinaryWriter writer, object value, bool writeToken)
        {
            Write(writer, (T)value, writeToken);
        }

        public abstract T Read(BinaryReader reader);

        public abstract void Write(BinaryWriter writer, T value, bool writeToken);

        public override object ReadObject(SpanReader reader) => Read(reader);

        public override void WriteObject(SpanWriter writer, object value, bool writeToken)
        {
            Write(writer, (T)value, writeToken);
        }

        public virtual T Read(SpanReader reader) => throw new NotImplementedException();

        public virtual void Write(SpanWriter writer, T value, bool writeToken) => throw new NotImplementedException();

        public override void WriteObject(BinaryInvokerWriter writer, object value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            Write(writer, (T)value, info, map);
        }

        public override object ReadObject(BinaryInvokerReader reader, object value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            return Read(reader, (T)value, info, map);
        }

        public virtual void Write(BinaryInvokerWriter writer, T value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            Write(writer.Writer, value, true);
        }

        public virtual T Read(BinaryInvokerReader reader, T value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            return Read(reader.Reader);
        }

        #endregion

        #region Xml
        public override object ObjectFromString(string value) => FromString(value);

        public override string ObjectToString(object value) => ToString((T)value);

        public abstract T FromString(string value);

        public abstract string ToString(T value);

        public override void WriteObject(XmlInvokerWriter writer, object value, TypeSerializeInfo info)
        {
            Write(writer, (T)value, info);
        }

        public override object ReadObject(XmlInvokerReader reader, object value, TypeSerializeInfo info)
        {
            return Read(reader, (T)value, info);
        }

        public virtual void Write(XmlInvokerWriter writer, T value, TypeSerializeInfo info)
        {
            writer.Writer.WriteString(ToString(value));
        }

        public virtual T Read(XmlInvokerReader reader, T value, TypeSerializeInfo info)
        {
            var str = reader.Reader.NodeType == System.Xml.XmlNodeType.Attribute
                ? reader.Reader.ReadContentAsString()
                : reader.Reader.ReadElementContentAsString();
            return FromString(str);
        }

        #endregion

        #region Json
        public override void WriteObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            Write(writer, (T)value, options);
        }

        public override object ReadObject(ref Utf8JsonReader reader, object value, JsonSerializerOptions options)
        {
            return ReadObject(ref reader, value, options);
        }

        public virtual void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }

        public virtual T Read(ref Utf8JsonReader reader, T value, JsonSerializerOptions options)
        {
            return FromString(reader.GetString());
        }
        #endregion
    }
}
