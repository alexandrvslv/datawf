using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class ObjectSerializer<T> : ElementSerializer<T>
    {
        public override bool CanConvertString => false;

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => ToString((T)value);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(BinaryWriter writer, object value, bool writeToken) => ToBinary(writer, (T)value, writeToken);

        public override T Read(BinaryInvokerReader reader, T value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map)
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

        public override void Write(BinaryInvokerWriter writer, T value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map)
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

        public override T FromString(string value) => throw new NotSupportedException();

        public override string ToString(T value) => throw new NotSupportedException();
    }
}
