using System.Globalization;
using System.IO;

namespace DataWF.Common
{
    public class CharSerializer : NullableSerializer<char>
    {
        public static readonly UInt8Serializer Instance = new UInt8Serializer();

        public override BinaryToken BinaryToken => BinaryToken.Char;

        public override char Read(BinaryReader reader) => reader.ReadChar();

        public override void Write(BinaryWriter writer, char value, bool writeToken)
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

