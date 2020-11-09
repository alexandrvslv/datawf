using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DataWF.Common
{
    //https://github.com/cwensley/Portable.Xaml/blob/74d570bf7f75ab0c2fcdf4217e3019b3f24920e6/src/Portable.Xaml/Portable.Xaml.Markup/ValueSerializer.cs
    public class StringSerializer : ElementSerializer<string>
    {
        public static readonly StringSerializer Instance = new StringSerializer();

        public override object ConvertFromString(string value) => value;

        public override string ConvertToString(object value) => (string)value;

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((string)value, writer, writeToken);

        public override string FromBinary(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            if (length > 0)
            {
                return Encoding.UTF8.GetString(reader.ReadBytes(length));
            }
            return string.Empty;
        }

        public override void ToBinary(string value, BinaryWriter writer, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.String);
            }
            writer.Write(value.Length);
            if (value.Length > 0)
            {
                writer.Write(Encoding.UTF8.GetBytes(value));
            }
        }

        public override string FromString(string value) => value;

        public override string ToString(string value) => value;

    }
}
