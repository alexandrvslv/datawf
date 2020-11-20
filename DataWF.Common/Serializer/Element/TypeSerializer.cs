using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class TypeSerializer : ElementSerializer<Type>
    {
        public static readonly TypeSerializer Instance = new TypeSerializer();

        public override bool CanConvertString => true;

        public override Type FromBinary(BinaryReader reader) => FromString(StringSerializer.Instance.FromBinary(reader));

        public override Type FromString(string value) => TypeHelper.ParseType(value);

        public override void ToBinary(BinaryWriter writer, Type value, bool writeToken) => StringSerializer.Instance.ToBinary(writer, ToString(value), writeToken);

        public override string ToString(Type value) => TypeHelper.FormatBinary(value);
    }

    public class TypeShortSerializer : ElementSerializer<Type>
    {
        public static readonly TypeSerializer Instance = new TypeSerializer();

        public override bool CanConvertString => true;

        public override Type FromBinary(BinaryReader reader) => FromString(StringSerializer.Instance.FromBinary(reader));

        public override Type FromString(string value) => TypeHelper.ParseType(value);

        public override void ToBinary(BinaryWriter writer, Type value, bool writeToken) => StringSerializer.Instance.ToBinary(writer, ToString(value), writeToken);

        public override string ToString(Type value) => TypeHelper.FormatBinary(value, true);
    }
}
