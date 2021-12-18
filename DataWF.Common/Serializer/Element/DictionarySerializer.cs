using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public sealed class DictionarySerializer<T, K, V> : ObjectSerializer<T> where T : IDictionary<K, V>
    {
        public override bool CanConvertString => false;

        public override T Read(BinaryInvokerReader reader, T value, TypeSerializeInfo typeInfo, Dictionary<ushort, IPropertySerializeInfo> map)
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
                map = reader.ReadType(out typeInfo);
                token = reader.ReadToken();
            }
            typeInfo = typeInfo ?? reader.Serializer.GetTypeInfo<T>();

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
                value = (T)(typeInfo.ListConstructor?.Create(length) ?? typeInfo.Constructor.Create());
            }
            if (token == BinaryToken.ArrayEntry)
            {
                do
                {
                    reader.ReadToken();//ObjectBegin
                    reader.ReadToken();//ObjectEntry Key
                    reader.ReadSchemaIndex();//ObjectEntryIndex 0

                    var entryKey = reader.Read<K>(default(K), keyTypeInfo);

                    reader.ReadToken();//ObjectEntry Value
                    reader.ReadSchemaIndex();//ObjectEntryIndex 1

                    var entryValue = reader.Read<V>(default(V), valueTypeInfo);

                    value[entryKey] = entryValue;

                    reader.ReadToken();//ObjectEnd
                }
                while (reader.ReadToken() == BinaryToken.ArrayEntry);
            }
            return value;
        }

        public override void Write(BinaryInvokerWriter writer, T value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteArrayBegin();
            if (writer.Serializer.WriteSchema)
            {
                writer.WriteType(value.GetType());
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
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Comment)
                typeInfo = reader.ReadType(typeInfo);

            typeInfo = typeInfo ?? reader.Serializer.GetTypeInfo(value?.GetType() ?? typeof(T));

            if (value == null || value.GetType() != typeInfo.Type)
            {
                value = (T)typeInfo.Constructor?.Create();
            }
            reader.ReadAttributes(value, typeInfo);

            if (reader.IsEmptyElement)
            {
                return value;
            }

            var entry = new DictionaryItem<K, V>();
            var itemTypeInfo = reader.Serializer.GetTypeInfo(entry.GetType());

            while (reader.ReadNextElement())
            {
                reader.Read(entry, itemTypeInfo);
                value[entry.Key] = entry.Value;
                entry.Reset();
            }
            return value;
        }

        public override void Write(XmlInvokerWriter writer, T value, TypeSerializeInfo typeInfo)
        {
            typeInfo = typeInfo ?? writer.Serializer.GetTypeInfo(value.GetType());

            writer.WriteObject<T>(value, typeInfo);
            var entry = new DictionaryItem<K, V>();
            var itemTypeInfo = writer.Serializer.GetTypeInfo(entry.GetType());

            foreach (var item in value)
            {
                entry.Fill(item);
                writer.Write(entry, itemTypeInfo, "i", false);
            }
        }

        public override T FromString(string value) => throw new NotSupportedException();

        public override string ToString(T value) => throw new NotSupportedException();
    }

    public class DictionarySerializer<T> : ObjectSerializer<T> where T : IDictionary
    {
        public override bool CanConvertString => false;

        public override T Read(BinaryInvokerReader reader, T value, TypeSerializeInfo typeInfo, Dictionary<ushort, IPropertySerializeInfo> map)
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
                map = reader.ReadType(out typeInfo);
                token = reader.ReadToken();
            }
            typeInfo = typeInfo ?? reader.Serializer.GetTypeInfo(value?.GetType() ?? typeof(T));

            int length = 1;
            if (token == BinaryToken.ArrayLength)
            {
                length = reader.Reader.ReadInt32();
                token = reader.ReadToken();
            }

            if (value == null || value.GetType() != typeInfo.Type)
            {
                value = (T)(typeInfo.ListConstructor?.Create(length) ?? typeInfo.Constructor.Create());
            }
            if (token == BinaryToken.ArrayEntry)
            {
                do
                {
                    reader.ReadToken();//ObjectBegin
                    reader.ReadToken();//ObjectEntry Key
                    reader.ReadSchemaIndex();//ObjectEntryIndex 0

                    var entryKey = reader.Read(null);

                    reader.ReadToken();//ObjectEntry Value
                    reader.ReadSchemaIndex();//ObjectEntryIndex 1

                    var entryValue = reader.Read(null);

                    value[entryKey] = entryValue;

                    reader.ReadToken();//ObjectEnd
                }
                while (reader.ReadToken() == BinaryToken.ArrayEntry);
            }
            return value;
        }

        public override void Write(BinaryInvokerWriter writer, T value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteArrayBegin();
            if (writer.Serializer.WriteSchema)
            {
                writer.WriteType(value.GetType());
            }

            writer.WriteArrayLength(value.Count);
            foreach (DictionaryEntry item in value)
            {
                writer.WriteArrayEntry();

                writer.WriteObjectBegin();

                writer.WriteObjectEntry();
                writer.WriteSchemaIndex(0);
                writer.Write(item.Key);

                writer.WriteObjectEntry();
                writer.WriteSchemaIndex(1);
                writer.Write(item.Value);

                writer.WriteObjectEnd();
            }
            writer.WriteArrayEnd();
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
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Comment)
                typeInfo = reader.ReadType(typeInfo);
            typeInfo = typeInfo ?? reader.Serializer.GetTypeInfo(value?.GetType() ?? typeof(T));

            if (value == null || value.GetType() != typeInfo.Type)
            {
                value = (T)typeInfo.Constructor?.Create();
            }
            reader.ReadAttributes(value, typeInfo);

            if (reader.IsEmptyElement)
            {
                return value;
            }

            var entry = new DictionaryItem<object, object>();
            var itemTypeInfo = reader.Serializer.GetTypeInfo(entry.GetType());

            while (reader.ReadNextElement())
            {
                reader.Read(entry, itemTypeInfo);
                value[entry.Key] = entry.Value;
                entry.Reset();
            }
            return value;
        }

        public override void Write(XmlInvokerWriter writer, T value, TypeSerializeInfo typeInfo)
        {
            typeInfo = typeInfo ?? writer.Serializer.GetTypeInfo(value?.GetType() ?? typeof(T));
            writer.WriteObject<T>(value, typeInfo);
            var entry = new DictionaryItem<object, object>();
            var itemTypeInfo = writer.Serializer.GetTypeInfo(entry.GetType());

            foreach (DictionaryEntry item in value)
            {
                entry.Fill(item);
                writer.Write(entry, itemTypeInfo, "i", false);
            }
        }

        public override T FromString(string value) => throw new NotSupportedException();

        public override string ToString(T value) => throw new NotSupportedException();
    }
}
