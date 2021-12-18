using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace DataWF.Common
{

    public sealed class DoubleSerializer : NullableSerializer<double>
    {
        public static readonly DoubleSerializer Instance = new DoubleSerializer();

        public override BinaryToken BinaryToken => BinaryToken.Double;

        public override double Read(BinaryReader reader) => reader.ReadDouble();

        public override void Write(BinaryWriter writer, double value, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.Double);
            }
            writer.Write(value);
        }

        public override double FromString(string value) => double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0D;

        public override string ToString(double value) => value.ToString(CultureInfo.InvariantCulture);
    }


    public sealed class FloatSerializer : NullableSerializer<float>
    {
        public static readonly FloatSerializer Instance = new FloatSerializer();

        public override BinaryToken BinaryToken => BinaryToken.Float;

        public override float Read(BinaryReader reader) => reader.ReadSingle();

        public override void Write(BinaryWriter writer, float value, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.Float);
            }
            writer.Write(value);
        }

        public override float FromString(string value) => float.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0F;

        public override string ToString(float value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class DecimalSerializer : NullableSerializer<decimal>
    {
        public static readonly DecimalSerializer Instance = new DecimalSerializer();

        public override BinaryToken BinaryToken => BinaryToken.Decimal;

        public override decimal Read(BinaryReader reader) => reader.ReadDecimal();

        public override void Write(BinaryWriter writer, decimal value, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.Decimal);
            }
            writer.Write(value);
        }

        public override decimal FromString(string value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0M;

        public override string ToString(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
