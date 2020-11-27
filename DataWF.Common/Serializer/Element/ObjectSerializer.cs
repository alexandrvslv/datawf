using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class ObjectSerializer<T> : ElementSerializer<T>
    {
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
                reader.ReadType(out typeInfo, out map);
                token = reader.ReadToken();
            }

            typeInfo = typeInfo ?? reader.Serializer.GetTypeInfo<T>();

            map = map ?? reader.GetMap(typeInfo);

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

            map = map ?? writer.GetMap(typeInfo);
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
    }
}
