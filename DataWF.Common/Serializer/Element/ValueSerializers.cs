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

        public override object ConvertFromBinary(BinaryReader reader) => ConvertFromString(StringSerializer.Instance.FromBinary(reader));

        public override object ConvertFromString(string value) => Converter.ConvertFromInvariantString(value);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => StringSerializer.Instance.ToBinary(ConvertToString(value), writer, writeToken);

        public override string ConvertToString(object value) => Converter.ConvertToInvariantString(value);

        public override T FromBinary(BinaryReader reader) => (T)ConvertFromBinary(reader);

        public override T FromString(string value) => (T)ConvertFromString(value);

        public override void ToBinary(T value, BinaryWriter writer, bool writeToken) => ConvertToBinary(value, writer, writeToken);

        public override string ToString(T value) => ConvertToString(value);
    }
}
