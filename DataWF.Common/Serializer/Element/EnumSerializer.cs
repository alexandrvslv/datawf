using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace DataWF.Common
{
    public class EnumSerializer<T> : NullableSerializer<T> where T : struct
    {
        public override object ConvertFromString(string value) => Enum.TryParse<T>(value, out var result) ? result : default(T);

        public override string ConvertToString(object value) => value.ToString();

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((T)value, writer, writeToken);

        public override T FromBinary(BinaryReader reader) { var temp = reader.ReadInt32(); return Unsafe.As<int, T>(ref temp); }

        public override T FromString(string value) => Enum.TryParse<T>(value, out var result) ? result : default(T);

        public override void ToBinary(T value, BinaryWriter writer, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.Int32);
            }
            writer.Write(Unsafe.As<T, int>(ref value));
        }

        public override string ToString(T value) => value.ToString();
    }
}
