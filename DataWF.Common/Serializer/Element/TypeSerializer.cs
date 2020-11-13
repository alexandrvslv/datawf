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

        public override void ConvertToBinary(BinaryWriter writer, object value, bool writeToken) => ToBinary(writer, (Type)value, writeToken);

        public override Type FromBinary(BinaryReader reader) => FromString(StringSerializer.Instance.FromBinary(reader));

        public override Type FromString(string value) => TypeHelper.ParseType(value);

        public override void ToBinary(BinaryWriter writer, Type value, bool writeToken) => StringSerializer.Instance.ToBinary(writer, ToString(value), writeToken);

        public override string ToString(Type value) => TypeHelper.FormatBinary(value);
    }
}
