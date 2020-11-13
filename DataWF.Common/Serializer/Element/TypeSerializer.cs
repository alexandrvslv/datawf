using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class TypeSerializer : ElementSerializer<Type>
    {
        public static readonly TypeSerializer Instance = new TypeSerializer();

        public override bool CanConvertString => true;

        public override object ConvertFromString(string value) => TypeHelper.ParseType(value);

        public override string ConvertToString(object value) => TypeHelper.FormatBinary((Type)value);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((Type)value, writer, writeToken);

        public override Type FromBinary(BinaryReader reader) => FromString(StringSerializer.Instance.FromBinary(reader));

        public override Type FromString(string value) => TypeHelper.ParseType(value);

        public override void ToBinary(Type value, BinaryWriter writer, bool writeToken) => StringSerializer.Instance.ToBinary(ToString(value), writer, writeToken);

        public override string ToString(Type value) => TypeHelper.FormatBinary(value);
    }
}
