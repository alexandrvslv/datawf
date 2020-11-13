using System.IO;

namespace DataWF.Common
{
    public class CharArraySerializer : ElementSerializer<char[]>
    {
        public static readonly CharArraySerializer Instance = new CharArraySerializer();

        public override bool CanConvertString => true;

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => value.ToString();

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(BinaryWriter writer, object value, bool writeToken) => ToBinary(writer, (char[])value, writeToken);

        public override char[] FromBinary(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            return reader.ReadChars(length);
        }

        public override void ToBinary(BinaryWriter writer, char[] value, bool writeToken)
        {
            if (value == null)
            {
                writer.Write((byte)BinaryToken.Null);
                return;
            }
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.CharArray);
            }
            writer.Write(value.Length);
            writer.Write(value);
        }

        public override char[] FromString(string value) => value.ToCharArray();

        public override string ToString(char[] value) => new string(value);
    }
}
