using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class ObjectSerializer<T> : ElementSerializer<T>
    {
        public override bool CanConvertString => false;

        public override T Read(BinaryInvokerReader reader, T value, TypeSerializationInfo info, Dictionary<ushort, IPropertySerializationInfo> map)
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
                reader.ReadType(out info, out map);
                token = reader.ReadToken();
            }
            map = map ?? reader.GetMap(info);

            if (value == null || value.GetType() != info.Type)
            {
                value = (T)info.Constructor?.Create();
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

        public override void Write(BinaryInvokerWriter writer, T value, TypeSerializationInfo info, Dictionary<ushort, IPropertySerializationInfo> map)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteObjectBegin();
            map = map ?? writer.GetMap(info);
            if (map == null)
            {
                map = writer.WriteType(info);
            }
            foreach (var entry in map)
            {
                var property = entry.Value;

                writer.WriteProperty<T>(property, value, entry.Key);
            }
            writer.WriteObjectEnd();
        }

        public override T FromBinary(BinaryReader reader)
        {
            using (var invokerReader = new BinaryInvokerReader(reader))
            {
                return Read(invokerReader, default(T), invokerReader.Serializer.GetTypeInfo<T>(), null);
            }
        }

        public override void ToBinary(BinaryWriter writer, T value, bool writeToken)
        {
            using (var invokerWriter = new BinaryInvokerWriter(writer))
            {
                Write(invokerWriter, value, invokerWriter.Serializer.GetTypeInfo<T>(), null);
            }
        }

        public override T Read(XmlInvokerReader reader, T value, TypeSerializationInfo typeInfo)
        {
            if (reader.NodeType == System.Xml.XmlNodeType.Comment)
                typeInfo = reader.ReadType(typeInfo);
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

        public override void Write(XmlInvokerWriter writer, T value, TypeSerializationInfo typeInfo)
        {
            writer.WriteObject<T>(value, typeInfo);
        }

        public override void PropertyToString(XmlInvokerWriter writer, object element, IPropertySerializationInfo property)
        {
            if (property.PropertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                if (value != null)
                {
                    writer.WriteStart(property);
                    Write(writer, value, writer.Serializer.GetTypeInfo<T>());
                    writer.WriteEnd(property);
                }
            }
            else
            {
                var value = property.PropertyInvoker.GetValue(element);
                if (value != null)
                {
                    writer.WriteStart(property);
                    Write(writer, value, writer.Serializer.GetTypeInfo<T>());
                    writer.WriteEnd(property);
                }
            }
        }

        public override void PropertyToString<E>(XmlInvokerWriter writer, E element, IPropertySerializationInfo property)
        {
            if (property.PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                if (value != null)
                {
                    writer.WriteStart(property);
                    Write(writer, value, writer.Serializer.GetTypeInfo<T>());
                    writer.WriteEnd(property);
                }
            }
            else
            {
                PropertyToString(writer, (object)element, property);
            }
        }

        public override T FromString(string value) => throw new NotSupportedException();

        public override string ToString(T value) => throw new NotSupportedException();
    }
}
