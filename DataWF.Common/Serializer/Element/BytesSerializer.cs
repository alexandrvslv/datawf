using Portable.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace DataWF.Common
{
    public class BytesSerializer<T> : ElementSerializer<T> where T : IByteSerializable
    {
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
            using (var stream = new MemoryStream(Convert.FromBase64String(value)))
            using (var reader = new BinaryReader(stream))
            {
                return FromBinary(reader);
            }
        }

        public override string ToString(T value)
        {
            using (var stream = new MemoryStream())
            using (var reader = new BinaryWriter(stream))
            {
                return FromBinary(reader);
            }
        }
    }
}
