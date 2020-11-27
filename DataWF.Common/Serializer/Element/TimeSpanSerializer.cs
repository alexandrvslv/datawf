using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace DataWF.Common
{
    public class TimeSpanSerializer : NullableSerializer<TimeSpan>
    {
        public static readonly TimeSpanSerializer Instance = new TimeSpanSerializer();

        public TimeSpanSerializer() : base(false)
        { }

        public override BinaryToken BinaryToken => BinaryToken.TimeSpan;

        public override TimeSpan Read(SpanReader reader) => TimeSpan.FromTicks(reader.Read<long>());

        public override void Write(SpanWriter writer, TimeSpan value, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.TimeSpan);
            }
            writer.Write(value.Ticks);
        }

        public override TimeSpan Read(BinaryReader reader) => TimeSpan.FromTicks(reader.ReadInt64());

        public override void Write(BinaryWriter writer, TimeSpan value, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.TimeSpan);
            }
            writer.Write(value.Ticks);
        }

        public override TimeSpan FromString(string value) => TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var timeSpan) ? timeSpan : TimeSpan.MinValue;

        public override string ToString(TimeSpan value) => value.ToString();
    }
}
