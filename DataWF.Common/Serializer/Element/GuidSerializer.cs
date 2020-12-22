using System;
using System.IO;

namespace DataWF.Common
{
    public class GuidSerializer : NullableSerializer<Guid>
    {
        public static readonly GuidSerializer Instance = new GuidSerializer();

        public GuidSerializer() : base(false)
        {
            SizeOfType = 16;
        }

        public override BinaryToken BinaryToken => BinaryToken.Guid;

        public override bool CanConvertString => true;

        public override Guid FromString(string value) => Guid.TryParse(value, out var guid) ? guid : Guid.Empty;

        public override string ToString(Guid value) => value.ToString();

        public override Guid Read(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(16);
            return new Guid(bytes);
        }

        public override void Write(BinaryWriter writer, Guid value, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.Guid);
            }
            writer.Write(value.ToByteArray());
        }

    }
}