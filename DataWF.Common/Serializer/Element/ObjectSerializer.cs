using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DataWF.Common
{
    public class ObjectSerializer<T> : ElementSerializer<T>
    {
        private static TypeSerializeInfo typeInfo;
        public static TypeSerializeInfo TypeInfo => typeInfo ?? (typeInfo = BaseSerializer.GetCacheTypeInfo<T>());

        public override bool CanConvertString => false;

        public override T Read(BinaryInvokerReader reader, T value, TypeSerializeInfo typeInfo, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            var token = reader.ReadToken();
            if (token == BinaryToken.Null)
            {
                return default(T);
            }

            if (token == BinaryToken.ObjectBegin)
            {
                token = reader.ReadToken();
            }
            if (token == BinaryToken.SchemaBegin)
            {
                map = reader.ReadType(out typeInfo);
                token = reader.ReadToken();
            }

            typeInfo = typeInfo ?? reader.Serializer.GetTypeInfo<T>();

            map = map ?? reader.GetMap(typeInfo.Type);

            if (value == null || value.GetType() != typeInfo.Type)
            {
                value = (T)typeInfo.Constructor?.Create();
            }
            if (token == BinaryToken.ObjectEntry)
            {
                do
                {
                    reader.ReadProperty(value, map);
                }
                while (reader.ReadToken() == BinaryToken.ObjectEntry);
            }
            return value;
        }

        public override void Write(BinaryInvokerWriter writer, T value, TypeSerializeInfo typeInfo, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteObjectBegin();

            typeInfo = typeInfo ?? writer.Serializer.GetTypeInfo(value.GetType());

            map = map ?? writer.GetMap(typeInfo.Type);
            if (map == null)
            {
                map = writer.WriteType(typeInfo);
            }
            foreach (var entry in map)
            {
                var property = entry.Value;

                writer.WriteProperty<T>(property, value, entry.Key);
            }
            writer.WriteObjectEnd();
        }

        public override T Read(BinaryReader reader)
        {
            using (var invokerReader = new BinaryInvokerReader(reader))
            {
                return Read(invokerReader, default(T), invokerReader.Serializer.GetTypeInfo<T>(), null);
            }
        }

        public override void Write(BinaryWriter writer, T value, bool writeToken)
        {
            using (var invokerWriter = new BinaryInvokerWriter(writer))
            {
                Write(invokerWriter, value, invokerWriter.Serializer.GetTypeInfo<T>(), null);
            }
        }

        public override T Read(XmlInvokerReader reader, T value, TypeSerializeInfo typeInfo)
        {
            if (reader.NodeType == System.Xml.XmlNodeType.Comment)
                typeInfo = reader.ReadType(typeInfo);

            typeInfo = typeInfo ?? reader.Serializer.GetTypeInfo<T>();

            if (value == null || value.GetType() != typeInfo.Type)
            {
                value = (T)typeInfo.Constructor?.Create();
            }

            reader.ReadAttributes<T>(value, typeInfo);

            if (reader.IsEmptyElement)
            {
                return value;
            }

            while (reader.ReadNextElement())
            {
                reader.ReadElement<T>(value, typeInfo, null);
            }
            return value;
        }

        public override void Write(XmlInvokerWriter writer, T value, TypeSerializeInfo typeInfo)
        {
            typeInfo = typeInfo ?? writer.Serializer.GetTypeInfo(value?.GetType() ?? typeof(T));
            writer.WriteObject<T>(value, typeInfo);
        }

        public override T FromString(string value) => throw new NotSupportedException();

        public override string ToString(T value) => throw new NotSupportedException();


        #region Json
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            WriteObject(writer, value, options, TypeInfo);
        }

        private void WriteObject(Utf8JsonWriter writer, T element, JsonSerializerOptions options, TypeSerializeInfo typeInfo)
        {
            if (element == null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteStartObject();
            foreach (var property in typeInfo.XmlProperties)
            {
                property.Write(writer, element, options);
            }
            writer.WriteEndObject();
        }

        public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            return ReadObject(ref reader, options, TypeInfo);
        }

        private T ReadObject(ref Utf8JsonReader jreader, JsonSerializerOptions options, TypeSerializeInfo typeInfo)
        {
            var property = (IPropertySerializeInfo)null;
            var propertyType = (Type)null;
            var element = (T)typeInfo.Constructor?.Create();
            while (jreader.Read() && jreader.TokenType != JsonTokenType.EndObject)
            {
                if (jreader.TokenType == JsonTokenType.PropertyName)
                {
                    property = typeInfo.GetProperty(jreader.GetString());
                    propertyType = property?.DataType;
                }
                else
                {
                    if (property == null)
                    {
                        JsonSerializer.Deserialize(ref jreader, typeof(object), options);
                        continue;
                    }
                    property.Read(ref jreader, element, options);
                }
            }
            return element;
        }
        #endregion
    }
}
