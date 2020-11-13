using Portable.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace DataWF.Common
{
    public class DateTimeSerializer : NullableSerializer<DateTime>
    {
        public static readonly DateTimeSerializer Instance = new DateTimeSerializer();

        public override object ConvertFromString(string value) => long.TryParse(value, out var binary)
            ? DateTime.FromBinary(binary)
            : DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? result : DateTime.MinValue;

        public override string ConvertToString(object value) => ((DateTime)value).ToString("o");

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((DateTime)value, writer, writeToken);

        public override DateTime FromBinary(BinaryReader reader) => DateTime.FromBinary(reader.ReadInt64());

        public override DateTime FromString(string value) => long.TryParse(value, out var binary)
            ? DateTime.FromBinary(binary)
            : DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? result : DateTime.MinValue;

        public override void ToBinary(DateTime value, BinaryWriter writer, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.DateTime);
            }
            writer.Write(value.ToBinary());
        }

        public override string ToString(DateTime value) => value.ToString("o");
    }
}
