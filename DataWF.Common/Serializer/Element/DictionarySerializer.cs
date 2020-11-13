using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class DictionarySerializer<T, K, V> : ElementSerializer<T> where T : IDictionary<K, V>
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
            var keyTypeInfo = reader.Serializer.GetTypeInfo<K>();
            var valueTypeInfo = reader.Serializer.GetTypeInfo<V>();

            if (value == null)
            {
                value = (T)(info.ListConstructor?.Create(length) ?? info.Constructor.Create());
            }
            if (token == BinaryToken.ArrayEntry)
            {
                do
                {
                    reader.ReadToken();//ObjectBegin
                    reader.ReadToken();//ObjectEntry Key
                    reader.ReadIndex();//ObjectEntryIndex 0

                    var entryKey = reader.Read<K>(default(K), keyTypeInfo);

                    reader.ReadToken();//ObjectEntry Value
                    reader.ReadIndex();//ObjectEntryIndex 1

                    var entryValue = reader.Read<V>(default(V), valueTypeInfo);

                    value[entryKey] = entryValue;

                    reader.ReadToken();//ObjectEnd
                }
                while (reader.ReadToken() == BinaryToken.ArrayEntry);
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
            var keyTypeInfo = writer.Serializer.GetTypeInfo<K>();
            var valueTypeInfo = writer.Serializer.GetTypeInfo<V>();

            writer.WriteArrayLength(value.Count);
            foreach (var item in value)
            {
                writer.WriteArrayEntry();

                writer.WriteObjectBegin();

                writer.WriteObjectEntry();
                writer.WriteSchemaIndex(0);
                writer.Write(item.Key, keyTypeInfo);

                writer.WriteObjectEntry();
                writer.WriteSchemaIndex(1);
                writer.Write(item.Value, valueTypeInfo);

                writer.WriteObjectEnd();
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
