using Portable.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace DataWF.Common
{
    public class BytesSerializer<T> : ElementSerializer<T> where T : IBinarySerializable
    {
        public override bool CanConvertString => false;

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => value.ToString();

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(BinaryWriter writer, object value, bool writeToken) => ToBinary(writer, (T)value, writeToken);

        public override T FromBinary(BinaryReader reader)
        {
            var obj = (T)FormatterServices.GetUninitializedObject(typeof(T));
            obj.Deserialize(reader);
            return obj;
        }

        public override void ToBinary(BinaryWriter writer, T value, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.ByteArray);
            }
            value.Serialize(writer);
        }

        public override T FromString(string value)
        {
            var buffer = Convert.FromBase64String(value);
            var obj = (T)FormatterServices.GetUninitializedObject(typeof(T));
            obj.Deserialize(buffer);
            return obj;
        }

        public override string ToString(T value)
        {
            var buffer = value.Serialize();
            return Convert.ToBase64String(buffer);
        }
    }
}
