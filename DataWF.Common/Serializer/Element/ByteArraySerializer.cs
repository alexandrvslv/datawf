using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class ByteArraySerializer : ElementSerializer<byte[]>
    {
        public override bool CanConvertString => true;

        public static readonly ByteArraySerializer Instance = new ByteArraySerializer();

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => ToString((byte[])value);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(BinaryWriter writer, object value, bool writeToken) => ToBinary(writer, (byte[])value, writeToken);

        public override byte[] FromBinary(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            return reader.ReadBytes(length);
        }

        public override void ToBinary(BinaryWriter writer, byte[] value, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.ByteArray);
            }
            writer.Write(value.Length);
            writer.Write(value);
        }

        public override byte[] FromString(string value) => Convert.FromBase64String(value);

        public override string ToString(byte[] value) => Convert.ToBase64String(value);
    }

    public class GenericListSerializer<T, V> : ElementSerializer<T> where T : IList<V>
    {
        public override bool CanConvertString => false;

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => ToString((T)value);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(BinaryWriter writer, object value, bool writeToken) => ToBinary(writer, (T)value, writeToken);

        public override T FromBinary(BinaryReader reader)
        {
            using (var invokerReader = new BinaryInvokerReader(reader))
            {
                var info = invokerReader.Serializer.GetTypeInfo(typeof(T));
                var token = invokerReader.ReadToken();
                if (token == BinaryToken.ArrayBegin)
                    token = invokerReader.ReadToken();

                if (token == BinaryToken.SchemaBegin)
                {
                    invokerReader.ReadType(out info, out _);
                    token = invokerReader.ReadToken();
                }
                int length = 1;
                if (token == BinaryToken.ArrayLength)
                {
                    length = invokerReader.Reader.ReadInt32();
                    token = invokerReader.ReadToken();
                }

                var element = (T)(info.ListConstructor?.Create(length) ?? info.Constructor.Create());
                if (token == BinaryToken.ArrayEntry)
                {
                    if (info.ListIsArray)
                    {
                        int index = 0;
                        var itemTypeInfo = invokerReader.Serializer.GetTypeInfo(info.ListItemType);
                        do
                        {
                            element[index++] = invokerReader.Read(default(V), itemTypeInfo);
                        }
                        while (invokerReader.ReadToken() == BinaryToken.ArrayEntry);
                    }
                    else
                    {
                        var itemTypeInfo = invokerReader.Serializer.GetTypeInfo(info.ListItemType);
                        do
                        {
                            var newobj = invokerReader.Read(default(V), itemTypeInfo);
                            element.Add(newobj);
                        }
                        while (invokerReader.ReadToken() == BinaryToken.ArrayEntry);
                    }
                }
                return element;
            }
        }

        public override void ToBinary(T value, BinaryWriter writer, bool writeToken)
        {
            using (var invokerWriter = new BinaryInvokerWriter(writer))
            {
                invokerWriter.WriteArrayBegin();
                invokerWriter.WriteType(typeof(T));
                invokerWriter.WriteArrayLength(value.Count);
                foreach (var item in value)
                {
                    invokerWriter.Write(item);
                }
                invokerWriter.WriteArrayEnd();
            }
        }

        public override T FromString(string value) => throw new NotSupportedException();

        public override string ToString(T value) => throw new NotSupportedException();
    }
}
