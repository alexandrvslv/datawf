using System.Globalization;
using System.IO;

namespace DataWF.Common
{
    public class BoolSerializer : NullableSerializer<bool>
    {
        public static readonly BoolSerializer Instance = new BoolSerializer();

        public override bool FromBinary(BinaryReader reader) => reader.ReadBoolean();

        public override void ToBinary(BinaryWriter writer, bool value, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.Boolean);
            }
            writer.Write(value);
        }

        public override bool FromString(string value) => bool.TryParse(value, out var result) && result;

        public override string ToString(bool value) => value.ToString(CultureInfo.InvariantCulture);
    }
}

