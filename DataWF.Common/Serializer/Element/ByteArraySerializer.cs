using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class ByteArraySerializer : ElementSerializer<byte[]>
    {
        public static readonly ByteArraySerializer Instance = new ByteArraySerializer();

        public override bool CanConvertString => true;


        public override byte[] FromBinary(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            return reader.ReadBytes(length);
        }

        public override void ToBinary(BinaryWriter writer, byte[] value, bool writeToken)
        {
            if (value == null)
            {
                writer.Write((byte)BinaryToken.Null);
                return;
            }
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
