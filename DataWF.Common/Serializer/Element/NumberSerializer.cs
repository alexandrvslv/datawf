using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace DataWF.Common
{

    public class DoubleSerializer : NullableSerializer<double>
    {
        public static readonly DoubleSerializer Instance = new DoubleSerializer();

        public override double FromBinary(BinaryReader reader) => reader.ReadDouble();

        public override void ToBinary(BinaryWriter writer, double value, bool writeToken)
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


    public class FloatSerializer : NullableSerializer<float>
    {
        public static readonly FloatSerializer Instance = new FloatSerializer();

        public override float FromBinary(BinaryReader reader) => reader.ReadSingle();

        public override void ToBinary(BinaryWriter writer, float value, bool writeToken)
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

    public class DecimalSerializer : NullableSerializer<decimal>
    {
        public static readonly DecimalSerializer Instance = new DecimalSerializer();

        public override decimal FromBinary(BinaryReader reader) => reader.ReadDecimal();

        public override void ToBinary(BinaryWriter writer, decimal value, bool writeToken)
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
