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

        public DateTimeSerializer() : base(false)
        { }

        public override BinaryToken BinaryToken => BinaryToken.DateTime;

        public override DateTime Read(SpanReader reader) => DateTime.FromBinary(reader.Read<long>());

        public override void Write(SpanWriter writer, DateTime value, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.DateTime);
            }
            writer.Write(value.ToBinary());
        }

        public override DateTime Read(BinaryReader reader) => DateTime.FromBinary(reader.ReadInt64());

        public override void Write(BinaryWriter writer, DateTime value, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.DateTime);
            }
            writer.Write(value.ToBinary());
        }

        public override DateTime FromString(string value) => long.TryParse(value, out var binary)
            ? DateTime.FromBinary(binary)
            : DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? result : DateTime.MinValue;

        public override string ToString(DateTime value) => value.ToString("o");
    }
}
