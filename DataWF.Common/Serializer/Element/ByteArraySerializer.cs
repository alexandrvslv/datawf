using System;
using System.IO;

namespace DataWF.Common
{
    public class ByteArraySerializer : ElementSerializer<byte[]>
    {
        public static readonly ByteArraySerializer Instance = new ByteArraySerializer();

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => ToString((byte[])value);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((byte[])value, writer, writeToken);

        public override byte[] FromBinary(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            return reader.ReadBytes(length);
        }

        public override void ToBinary(byte[] value, BinaryWriter writer, bool writeToken)
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
}
