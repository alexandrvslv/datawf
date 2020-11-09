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

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((T)value, writer, writeToken);

        public override T FromBinary(BinaryReader reader)
        {
            var obj = (T)FormatterServices.GetUninitializedObject(typeof(T));
            obj.Deserialize(reader);
            return obj;
        }

        public override T FromString(string value) => throw new NotImplementedException();

        public override void ToBinary(T value, BinaryWriter writer, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.ByteArray);
            }
            value.Serialize(writer);
        }

        public override string ToString(T value) => value.ToString();
    }
}
