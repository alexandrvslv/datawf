using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace DataWF.Common
{
    public class Int32Serializer : StructSerializer<int>
    {
        public static readonly Int32Serializer Instance = new Int32Serializer();

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => ToString((int)value);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((int)value, writer, writeToken);

        public override int FromBinary(BinaryReader reader) => reader.ReadInt32();

        public override void ToBinary(int value, BinaryWriter writer, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.Int32);
            }
            writer.Write(value);
        }

        public override int FromString(string value) => int.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0;

        public override string ToString(int value) => value.ToString(CultureInfo.InvariantCulture);
    }


    public class UInt32Serializer : StructSerializer<uint>
    {
        public static readonly UInt32Serializer Instance = new UInt32Serializer();

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => ToString((uint)value);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((uint)value, writer, writeToken);

        public override uint FromBinary(BinaryReader reader) => reader.ReadUInt32();

        public override void ToBinary(uint value, BinaryWriter writer, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.UInt32);
            }
            writer.Write(value);
        }

        public override uint FromString(string value) => uint.TryParse(value, out var result) ? result : (uint)0;

        public override string ToString(uint value) => value.ToString(CultureInfo.InvariantCulture);


    }

    public class Int64Serializer : StructSerializer<long>
    {
        public static readonly Int64Serializer Instance = new Int64Serializer();

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => ToString((long)value);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((long)value, writer, writeToken);

        public override long FromBinary(BinaryReader reader) => reader.ReadInt64();

        public override void ToBinary(long value, BinaryWriter writer, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.Int64);
            }
            writer.Write(value);
        }

        public override long FromString(string value) => long.TryParse(value, out var result) ? result : 0L;

        public override string ToString(long value) => value.ToString(CultureInfo.InvariantCulture);


    }

    public class UInt64Serializer : StructSerializer<ulong>
    {
        public static readonly UInt64Serializer Instance = new UInt64Serializer();

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => ToString((ulong)value);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((ulong)value, writer, writeToken);

        public override ulong FromBinary(BinaryReader reader) => reader.ReadUInt64();

        public override void ToBinary(ulong value, BinaryWriter writer, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.UInt64);
            }
            writer.Write(value);
        }

        public override ulong FromString(string value) => ulong.TryParse(value, out var result) ? result : (ulong)0;

        public override string ToString(ulong value) => value.ToString(CultureInfo.InvariantCulture);


    }

    public class Int16Serializer : StructSerializer<short>
    {
        public static readonly Int16Serializer Instance = new Int16Serializer();

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => ToString((short)value);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((short)value, writer, writeToken);

        public override short FromBinary(BinaryReader reader) => reader.ReadInt16();

        public override void ToBinary(short value, BinaryWriter writer, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.Int16);
            }
            writer.Write(value);
        }

        public override short FromString(string value) => short.TryParse(value, out var result) ? result : (short)0;

        public override string ToString(short value) => value.ToString(CultureInfo.InvariantCulture);


    }

    public class UInt16Serializer : StructSerializer<ushort>
    {
        public static readonly UInt16Serializer Instance = new UInt16Serializer();

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => ToString((ushort)value);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((ushort)value, writer, writeToken);

        public override ushort FromBinary(BinaryReader reader) => reader.ReadUInt16();

        public override void ToBinary(ushort value, BinaryWriter writer, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.UInt16);
            }
            writer.Write(value);
        }

        public override ushort FromString(string value) => ushort.TryParse(value, out var result) ? result : (ushort)0;

        public override string ToString(ushort value) => value.ToString(CultureInfo.InvariantCulture);


    }

    public class Int8Serializer : StructSerializer<sbyte>
    {
        public static readonly Int8Serializer Instance = new Int8Serializer();

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => ToString((sbyte)value);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((sbyte)value, writer, writeToken);

        public override sbyte FromBinary(BinaryReader reader) => reader.ReadSByte();

        public override void ToBinary(sbyte value, BinaryWriter writer, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.Int8);
            }
            writer.Write(value);
        }

        public override sbyte FromString(string value) => sbyte.TryParse(value, out var result) ? result : (sbyte)0;

        public override string ToString(sbyte value) => value.ToString(CultureInfo.InvariantCulture);


    }

    public class UInt8Serializer : StructSerializer<byte>
    {
        public static readonly UInt8Serializer Instance = new UInt8Serializer();

        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => ToString((byte)value);

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(object value, BinaryWriter writer, bool writeToken) => ToBinary((byte)value, writer, writeToken);

        public override byte FromBinary(BinaryReader reader) => reader.ReadByte();

        public override void ToBinary(byte value, BinaryWriter writer, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.UInt8);
            }
            writer.Write(value);
        }

        public override byte FromString(string value) => byte.TryParse(value, out var result) ? result : (byte)0;

        public override string ToString(byte value) => value.ToString(CultureInfo.InvariantCulture);

    }
}

