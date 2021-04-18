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
        public int IntId { get => GetProperty<int>(); set => SetProperty(value); }

        [Column("int_unsigned")]
        public uint IntUnsigned { get => GetProperty<uint>(); set => SetProperty(value); }

        [Column("int_nullable")]
        public int? IntNullable { get => GetProperty<int?>(); set => SetProperty(value); }

        [Column("short_value")]
        public short Short { get => GetProperty<short>(); set => SetProperty(value); }

        [Column("short_unsigned")]
        public ushort ShortUnsigned { get => GetProperty<ushort>(); set => SetProperty(value); }

        [Column("short_nullable")]
        public short? ShortNullable { get => GetProperty<short?>(); set => SetProperty(value); }

        [Column("long_value")]
        public long Long { get => GetProperty<long>(); set => SetProperty(value); }

        [Column("long_nullable")]
        public long? LongNullable { get => GetProperty<long?>(); set => SetProperty(value); }

        [Column("byte_value")]
        public byte Byte { get => GetProperty<byte>(); set => SetProperty(value); }

        [Column("byte_signed")]
        public sbyte ByteSigned { get => GetProperty<sbyte>(); set => SetProperty(value); }

        [Column("byte_nullable")]
        public byte? ByteNullable { get => GetProperty<byte?>(); set => SetProperty(value); }

        [Column("float_value")]
        public float Float { get => GetProperty<float>(); set => SetProperty(value); }

        [Column("float_nullable")]
        public float? FloatNullable { get => GetProperty<float?>(); set => SetProperty(value); }

        [Column("double_value")]
        public double Double { get => GetProperty<double>(); set => SetProperty(value); }

        [Column("double_nullable")]
        public double? DoubleNullable { get => GetProperty<double?>(); set => SetProperty(value); }

        [Column("decimal_value", Size = 29)]
        public decimal Decimal { get => GetProperty<decimal>(); set => SetProperty(value); }

        [Column("decimal_nullable", Size = 29)]
        public decimal? DecimalNullable { get => GetProperty<decimal?>(); set => SetProperty(value); }

        [Column("bool_value")]
        public bool Bool { get => GetProperty<bool>(); set => SetProperty(value); }

        [Column("bool_nullable")]
        public bool? BoolNullable { get => GetProperty<bool?>(); set => SetProperty(value); }

        [Column("datetime_value")]
        public DateTime DateTime { get => GetProperty<DateTime>(); set => SetProperty(value); }

        [Column("datetime_nullable")]
        public DateTime? DateTimeNullable { get => GetProperty<DateTime?>(); set => SetProperty(value); }

        [Column("typespan_value")]
        public TimeSpan TimeSpan { get => GetProperty<TimeSpan>(); set => SetProperty(value); }

        [Column("timespan_nullable")]
        public TimeSpan? TimeSpanNullable { get => GetProperty<TimeSpan?>(); set => SetProperty(value); }

        [Column("enum_long")]
        public Int64Enum EnumLong { get => GetProperty<Int64Enum>(); set => SetProperty(value); }

        [Column("enum_long_nullable")]
        public Int64Enum? EnumLongNullable { get => GetProperty<Int64Enum?>(); set => SetProperty(value); }

        [Column("enum_int")]
        public Int32Enum EnumInt { get => GetProperty<Int32Enum>(); set => SetProperty(value); }

        [Column("enum_int_unsigned")]
        public UInt32Enum EnumIntUnsigned { get => GetProperty<UInt32Enum>(); set => SetProperty(value); }

        [Column("enum_int_nullable")]
        public Int32Enum? EnumIntNullable { get => GetProperty<Int32Enum?>(); set => SetProperty(value); }

        [Column("enum_short")]
        public Int16Enum EnumShort { get => GetProperty<Int16Enum>(); set => SetProperty(value); }

        [Column("enum_short_unsigned")]
        public UInt16Enum EnumShortUnsigned { get => GetProperty<UInt16Enum>(); set => SetProperty(value); }

        [Column("enum_short_nullable")]
        public Int16Enum? EnumShortNullable { get => GetProperty<Int16Enum?>(); set => SetProperty(value); }

        [Column("enum_byte")]
        public UInt8Enum EnumByte { get => GetProperty<UInt8Enum>(); set => SetProperty(value); }

        [Column("enum_byte_signed")]
        public Int8Enum EnumByteSigned { get => GetProperty<Int8Enum>(); set => SetProperty(value); }

        [Column("enum_byte_nullable")]
        public UInt8Enum? EnumByteNullable { get => GetProperty<UInt8Enum?>(); set => SetProperty(value); }

        [Column("string_value")]
        public string String { get => GetProperty<string>(); set => SetProperty(value); }

        [Column("bytearray_value")]
        public byte[] ByteArray { get => GetProperty<byte[]>(); set => SetProperty(value); }
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
