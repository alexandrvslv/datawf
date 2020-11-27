using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class TypeSerializer : ElementSerializer<Type>
    {
        public static readonly TypeSerializer Instance = new TypeSerializer();

        public override bool CanConvertString => true;

        public override Type Read(BinaryReader reader) => FromString(StringSerializer.Instance.Read(reader));

        public override Type FromString(string value) => TypeHelper.ParseType(value);

        public override void Write(BinaryWriter writer, Type value, bool writeToken) => StringSerializer.Instance.Write(writer, ToString(value), writeToken);

        public override string ToString(Type value) => TypeHelper.FormatBinary(value);
    }

    public class TypeShortSerializer : ElementSerializer<Type>
    {
        public static readonly TypeSerializer Instance = new TypeSerializer();

        public override bool CanConvertString => true;

        public override Type Read(BinaryReader reader) => FromString(StringSerializer.Instance.Read(reader));

        public override Type FromString(string value) => TypeHelper.ParseType(value);

        public override void Write(BinaryWriter writer, Type value, bool writeToken) => StringSerializer.Instance.Write(writer, ToString(value), writeToken);

        public override string ToString(Type value) => TypeHelper.FormatBinary(value, true);
    }
}
