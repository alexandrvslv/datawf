using System.Globalization;
using System.IO;

namespace DataWF.Common
{
    public class BoolSerializer : StructSerializer<bool>
    {
        public static readonly BoolSerializer Instance = new BoolSerializer();

        public override object ConvertFromString(string value) => bool.TryParse(value, out var result) ? result : false;

        public override string ConvertToString(object value) => ((bool)value).ToString(CultureInfo.InvariantCulture);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((bool)value, writer, writeToken);

        public override bool FromBinary(BinaryReader reader) => reader.ReadBoolean();

        public override void ToBinary(bool value, BinaryWriter writer, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.Boolean);
            }
            writer.Write(value);
        }

        public override bool FromString(string value) => bool.TryParse(value, out var result) ? result : false;

        public override string ToString(bool value) => value.ToString(CultureInfo.InvariantCulture);
    }
}

