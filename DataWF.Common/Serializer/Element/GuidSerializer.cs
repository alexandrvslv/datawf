using System;
using System.IO;

namespace DataWF.Common
{
    public class GuidSerializer : NullableSerializer<Guid>
    {
        public static readonly GuidSerializer Instance = new GuidSerializer();

        public override bool CanConvertString => true;

        public override Guid FromString(string value) => Guid.TryParse(value, out var guid) ? guid : Guid.Empty;

        public override string ToString(Guid value) => value.ToString();

        public override Guid FromBinary(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(16);
            return new Guid(bytes);
        }

        public override void ToBinary(BinaryWriter writer, Guid value, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.Guid);
            }
            writer.Write(value.ToByteArray());
        }

    }
}