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

        public override bool CanConvertString => true;

        public override string FromBinary(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            if (length > 0)
            {
                return Encoding.UTF8.GetString(reader.ReadBytes(length));
            }
            return string.Empty;
        }

        public override void ToBinary(BinaryWriter writer, string value, bool writeToken)
        {
            if (value == null)
            {
                writer.Write((byte)BinaryToken.Null);
                return;
            }
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.String);
            }

            if (value.Length > 0)
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            else
            {
                writer.Write(0);
            }
        }

        public override string FromString(string value) => value;

        public override string ToString(string value) => value;

    }
}
