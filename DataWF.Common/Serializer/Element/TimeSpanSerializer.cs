using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace DataWF.Common
{
    public class TimeSpanSerializer : NullableSerializer<TimeSpan>
    {
        public static readonly TimeSpanSerializer Instance = new TimeSpanSerializer();

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => ToString((TimeSpan)value);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((TimeSpan)value, writer, writeToken);

        public override TimeSpan FromBinary(BinaryReader reader) => TimeSpan.FromTicks(reader.ReadInt64());

        public override TimeSpan FromString(string value) => TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var timeSpan) ? timeSpan : TimeSpan.MinValue;

        public override void ToBinary(TimeSpan value, BinaryWriter writer, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.TimeSpan);
            }
            writer.Write(value.Ticks);
        }

        public override string ToString(TimeSpan value) => value.ToString();
    }
}
