using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace DataWF.Common
{
    public sealed class TypeConverterSerializers<T> : ElementSerializer<T>
    {
        public TypeConverterSerializers(TypeConverter converter)
        {
            Converter = converter;
        }

        public TypeConverter Converter { get; set; }

        public override bool CanConvertString => true;

        public override object ReadObject(BinaryReader reader) => FromString(StringSerializer.Instance.Read(reader));

        public override void WriteObject(BinaryWriter writer, object value, bool writeToken) => StringSerializer.Instance.Write(writer, ObjectToString(value), writeToken);

        public override T Read(BinaryReader reader) => (T)Read(reader);

        public override void Write(BinaryWriter writer, T value, bool writeToken) => WriteObject(writer, (object)value, writeToken);

        public override string ObjectToString(object value) => Converter.ConvertToInvariantString(value);

        public override object ObjectFromString(string value) => Converter.ConvertFromInvariantString(value);

        public override T FromString(string value) => (T)ObjectFromString(value);

        public override string ToString(T value) => ObjectToString((object)value);


    }
}
