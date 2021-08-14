using DataWF.Data;
using System;

namespace DataWF.Test.Data
{
    [Table(TestORM.TestColumnsTableName, "Default")]
    public partial class TestColumns : DBItem
    {
        public TestColumns(DBTable table) : base(table)
        {
        }

        [Column("int_value", Keys = DBColumnKeys.Primary | DBColumnKeys.Notnull)]
        public int IntId { get => GetValue<int>(); set => SetValue(value); }

        [Column("int_unsigned")]
        public uint IntUnsigned { get => GetValue<uint>(); set => SetValue(value); }

        [Column("int_nullable")]
        public int? IntNullable { get => GetValue<int?>(); set => SetValue(value); }

        [Column("short_value")]
        public short Short { get => GetValue<short>(); set => SetValue(value); }

        [Column("short_unsigned")]
        public ushort ShortUnsigned { get => GetValue<ushort>(); set => SetValue(value); }

        [Column("short_nullable")]
        public short? ShortNullable { get => GetValue<short?>(); set => SetValue(value); }

        [Column("long_value")]
        public long Long { get => GetValue<long>(); set => SetValue(value); }

        [Column("long_nullable")]
        public long? LongNullable { get => GetValue<long?>(); set => SetValue(value); }

        [Column("byte_value")]
        public byte Byte { get => GetValue<byte>(); set => SetValue(value); }

        [Column("byte_signed")]
        public sbyte ByteSigned { get => GetValue<sbyte>(); set => SetValue(value); }

        [Column("byte_nullable")]
        public byte? ByteNullable { get => GetValue<byte?>(); set => SetValue(value); }

        [Column("float_value")]
        public float Float { get => GetValue<float>(); set => SetValue(value); }

        [Column("float_nullable")]
        public float? FloatNullable { get => GetValue<float?>(); set => SetValue(value); }

        [Column("double_value")]
        public double Double { get => GetValue<double>(); set => SetValue(value); }

        [Column("double_nullable")]
        public double? DoubleNullable { get => GetValue<double?>(); set => SetValue(value); }

        [Column("decimal_value", Size = 29)]
        public decimal Decimal { get => GetValue<decimal>(); set => SetValue(value); }

        [Column("decimal_nullable", Size = 29)]
        public decimal? DecimalNullable { get => GetValue<decimal?>(); set => SetValue(value); }

        [Column("bool_value")]
        public bool Bool { get => GetValue<bool>(); set => SetValue(value); }

        [Column("bool_nullable")]
        public bool? BoolNullable { get => GetValue<bool?>(); set => SetValue(value); }

        [Column("datetime_value")]
        public DateTime DateTime { get => GetValue<DateTime>(); set => SetValue(value); }

        [Column("datetime_nullable")]
        public DateTime? DateTimeNullable { get => GetValue<DateTime?>(); set => SetValue(value); }

        [Column("typespan_value")]
        public TimeSpan TimeSpan { get => GetValue<TimeSpan>(); set => SetValue(value); }

        [Column("timespan_nullable")]
        public TimeSpan? TimeSpanNullable { get => GetValue<TimeSpan?>(); set => SetValue(value); }

        [Column("enum_long")]
        public Int64Enum EnumLong { get => GetValue<Int64Enum>(); set => SetValue(value); }

        [Column("enum_long_nullable")]
        public Int64Enum? EnumLongNullable { get => GetValue<Int64Enum?>(); set => SetValue(value); }

        [Column("enum_int")]
        public Int32Enum EnumInt { get => GetValue<Int32Enum>(); set => SetValue(value); }

        [Column("enum_int_unsigned")]
        public UInt32Enum EnumIntUnsigned { get => GetValue<UInt32Enum>(); set => SetValue(value); }

        [Column("enum_int_nullable")]
        public Int32Enum? EnumIntNullable { get => GetValue<Int32Enum?>(); set => SetValue(value); }

        [Column("enum_short")]
        public Int16Enum EnumShort { get => GetValue<Int16Enum>(); set => SetValue(value); }

        [Column("enum_short_unsigned")]
        public UInt16Enum EnumShortUnsigned { get => GetValue<UInt16Enum>(); set => SetValue(value); }

        [Column("enum_short_nullable")]
        public Int16Enum? EnumShortNullable { get => GetValue<Int16Enum?>(); set => SetValue(value); }

        [Column("enum_byte")]
        public UInt8Enum EnumByte { get => GetValue<UInt8Enum>(); set => SetValue(value); }

        [Column("enum_byte_signed")]
        public Int8Enum EnumByteSigned { get => GetValue<Int8Enum>(); set => SetValue(value); }

        [Column("enum_byte_nullable")]
        public UInt8Enum? EnumByteNullable { get => GetValue<UInt8Enum?>(); set => SetValue(value); }

        [Column("string_value")]
        public string String { get => GetValue<string>(); set => SetValue(value); }

        [Column("bytearray_value")]
        public byte[] ByteArray { get => GetValue<byte[]>(); set => SetValue(value); }
    }

    public enum Int64Enum : long
    {
        Min = long.MinValue,
        Default = default(long),
        Max = long.MaxValue
    }

    public enum Int32Enum : int
    {
        Min = int.MinValue,
        Default = default(int),
        Max = int.MaxValue
    }

    public enum Int16Enum : short
    {
        Min = short.MinValue,
        Default = default(short),
        Max = short.MaxValue
    }

    public enum Int8Enum : sbyte
    {
        Min = sbyte.MinValue,
        Default = default(sbyte),
        Max = sbyte.MaxValue,
    }

    public enum UInt32Enum : uint
    {
        Min = uint.MinValue,
        Max = uint.MaxValue
    }

    public enum UInt16Enum : ushort
    {
        Min = ushort.MinValue,
        Max = ushort.MaxValue
    }

    public enum UInt8Enum : byte
    {
        Min = byte.MinValue,
        Max = byte.MaxValue
    }
}
