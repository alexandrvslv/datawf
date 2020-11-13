using System.Globalization;
using System.IO;

namespace DataWF.Common
{
    public class CharSerializer : NullableSerializer<char>
    {
        public static readonly UInt8Serializer Instance = new UInt8Serializer();

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => ToString((char)value);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((char)value, writer, writeToken);

        public override char FromBinary(BinaryReader reader) => reader.ReadChar();

        public override void ToBinary(char value, BinaryWriter writer, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.Char);
            }
            writer.Write(value);
        }

        public override char FromString(string value) => char.TryParse(value, out var result) ? result : (char)0;

        public override string ToString(char value) => value.ToString(CultureInfo.InvariantCulture);


    }
}

