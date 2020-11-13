using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class ListSerializer<T, V> : ElementSerializer<T> where T : IList<V>
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

            if (token == BinaryToken.ArrayBegin)
            {
                token = reader.ReadToken();
            }

            if (token == BinaryToken.SchemaBegin)
            {
                reader.ReadType(out info, out _);
                token = reader.ReadToken();
            }
            int length = 1;
            if (token == BinaryToken.ArrayLength)
            {
                length = reader.Reader.ReadInt32();
                token = reader.ReadToken();
            }
            var valueTypeInfo = reader.Serializer.GetTypeInfo<V>();
            if (value == null)
            {
                value = (T)(info.ListConstructor?.Create(length) ?? info.Constructor.Create());
            }
            if (token == BinaryToken.ArrayEntry)
            {
                if (info.ListIsArray)
                {
                    int index = 0;
                    do
                    {
                        value[index++] = reader.Read(default(V), valueTypeInfo);
                    }
                    while (reader.ReadToken() == BinaryToken.ArrayEntry);
                }
                else
                {
                    do
                    {
                        var newobj = reader.Read(default(V), valueTypeInfo);
                        value.Add(newobj);
                    }
                    while (reader.ReadToken() == BinaryToken.ArrayEntry);
                }
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
            writer.WriteArrayBegin();
            if (writer.Serializer.WriteSchema)
            {
                writer.WriteType(typeof(T));
            }
            var valueTypeInfo = writer.Serializer.GetTypeInfo<V>();
            writer.WriteArrayLength(value.Count);
            foreach (var item in value)
            {
                writer.WriteArrayEntry();
                writer.Write(item, valueTypeInfo);
            }
            writer.WriteArrayEnd();
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
