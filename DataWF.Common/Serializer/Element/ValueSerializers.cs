using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace DataWF.Common
{
    public class TypeConverterSerializers<T> : ElementSerializer<T>
    {
        public TypeConverterSerializers(TypeConverter converter)
        {
            Converter = converter;
        }

        public TypeConverter Converter { get; set; }

        public override bool CanConvertString => true;

        public override object ConvertFromBinary(BinaryReader reader) => ConvertFromString(StringSerializer.Instance.FromBinary(reader));

        public override object ConvertFromString(string value) => Converter.ConvertFromInvariantString(value);

        public override void ConvertToBinary(BinaryWriter writer, object value, bool writeToken) => StringSerializer.Instance.ToBinary(writer, ConvertToString(value), writeToken);

        public override string ConvertToString(object value) => Converter.ConvertToInvariantString(value);

        public override T FromBinary(BinaryReader reader) => (T)ConvertFromBinary(reader);

        public override T FromString(string value) => (T)ConvertFromString(value);

        public override void ToBinary(BinaryWriter writer, T value, bool writeToken) => ConvertToBinary(writer, value, writeToken);

        public override string ToString(T value) => ConvertToString(value);
    }
}
