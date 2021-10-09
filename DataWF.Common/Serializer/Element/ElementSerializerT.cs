using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.Common
{
    public abstract class ElementSerializer<T> : JsonConverter<T>, IElementSerializer<T>
    {
        public int SizeOfType { get; protected set; }
        public abstract bool CanConvertString { get; }
        #region Binary
        public virtual object ReadObject(BinaryReader reader) => Read(reader);

        public virtual void WriteObject(BinaryWriter writer, object value, bool writeToken)
        {
            Write(writer, (T)value, writeToken);
        }

        public abstract T Read(BinaryReader reader);

        public abstract void Write(BinaryWriter writer, T value, bool writeToken);

        public virtual object ReadObject(SpanReader reader) => Read(reader);

        public virtual void WriteObject(SpanWriter writer, object value, bool writeToken)
        {
            Write(writer, (T)value, writeToken);
        }

        public virtual T Read(SpanReader reader) => throw new NotImplementedException();

        public virtual void Write(SpanWriter writer, T value, bool writeToken) => throw new NotImplementedException();

        public virtual void WriteObject(BinaryInvokerWriter writer, object value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            Write(writer, (T)value, info, map);
        }

        public virtual object ReadObject(BinaryInvokerReader reader, object value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
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
        public virtual object ObjectFromString(string value) => FromString(value);

        public virtual string ObjectToString(object value) => ToString((T)value);

        public abstract T FromString(string value);

        public abstract string ToString(T value);

        public virtual void WriteObject(XmlInvokerWriter writer, object value, TypeSerializeInfo info)
        {
            Write(writer, (T)value, info);
        }

        public virtual object ReadObject(XmlInvokerReader reader, object value, TypeSerializeInfo info)
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

        public override bool CanConvert(Type typeToConvert)
        {
            return base.CanConvert(typeToConvert);
        }

        public virtual void WriteObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            Write(writer, (T)value, options);
        }

        public virtual object ReadObject(ref Utf8JsonReader reader, object value, JsonSerializerOptions options)
        {
            return ReadObject(ref reader, value, options);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize<T>(writer, value, options);
        }

        public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<T>(ref reader, options);
            
        }
        #endregion
    }
}
