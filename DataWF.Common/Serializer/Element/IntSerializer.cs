using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace DataWF.Common
{
    public sealed class Int32Serializer : NullableSerializer<int>
    {
        public static readonly Int32Serializer Instance = new Int32Serializer();

        public override BinaryToken BinaryToken => BinaryToken.Int32;

        public override int Read(BinaryReader reader) => reader.ReadInt32();

        public override void Write(BinaryWriter writer, int value, bool writeToken)
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


    public sealed class UInt32Serializer : NullableSerializer<uint>
    {
        public static readonly UInt32Serializer Instance = new UInt32Serializer();

        public override BinaryToken BinaryToken => BinaryToken.UInt32;

        public override uint Read(BinaryReader reader) => reader.ReadUInt32();

        public override void Write(BinaryWriter writer, uint value, bool writeToken)
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

    public sealed class Int64Serializer : NullableSerializer<long>
    {
        public static readonly Int64Serializer Instance = new Int64Serializer();

        public override BinaryToken BinaryToken => BinaryToken.Int64;

        public override long Read(BinaryReader reader) => reader.ReadInt64();

        public override void Write(BinaryWriter writer, long value, bool writeToken)
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

    public sealed class UInt64Serializer : NullableSerializer<ulong>
    {
        public static readonly UInt64Serializer Instance = new UInt64Serializer();

        public override BinaryToken BinaryToken => BinaryToken.UInt64;

        public override ulong Read(BinaryReader reader) => reader.ReadUInt64();

        public override void Write(BinaryWriter writer, ulong value, bool writeToken)
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

    public sealed class Int16Serializer : NullableSerializer<short>
    {
        public static readonly Int16Serializer Instance = new Int16Serializer();

        public override BinaryToken BinaryToken => BinaryToken.Int16;

        public override short Read(BinaryReader reader) => reader.ReadInt16();

        public override void Write(BinaryWriter writer, short value, bool writeToken)
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

    public sealed class UInt16Serializer : NullableSerializer<ushort>
    {
        public static readonly UInt16Serializer Instance = new UInt16Serializer();

        public override BinaryToken BinaryToken => BinaryToken.UInt16;

        public override ushort Read(BinaryReader reader) => reader.ReadUInt16();

        public override void Write(BinaryWriter writer, ushort value, bool writeToken)
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

    public sealed class Int8Serializer : NullableSerializer<sbyte>
    {
        public static readonly Int8Serializer Instance = new Int8Serializer();

        public override BinaryToken BinaryToken => BinaryToken.Int8;

        public override sbyte Read(BinaryReader reader) => reader.ReadSByte();

        public override void Write(BinaryWriter writer, sbyte value, bool writeToken)
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

    public sealed class UInt8Serializer : NullableSerializer<byte>
    {
        public static readonly UInt8Serializer Instance = new UInt8Serializer();

        public override BinaryToken BinaryToken => BinaryToken.UInt8;

        public override byte Read(BinaryReader reader) => reader.ReadByte();

        public override void Write(BinaryWriter writer, byte value, bool writeToken)
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

