using System;
using System.Collections.Generic;
using DataWF.Invokers;

namespace DataWF.Test;

[Invoker(Instance = true)]
public partial class TestClass
{
    public TestClass()
        : this(3)
    {
    }

    public TestClass(int field)
    {
        Field = field;
        StringProperty = "Name";
        IntProperty = field;
        NIntProperty = field;
        IntArray = [1, field, 3];
        IntListProperty = [1, field, 3];
        Struct = new TestStruct()
        {
            Field = field,
            IntProperty = field,
            NIntProperty = field
        };
    }
    public int Field;

    public partial string StringProperty { get; set; }
    public partial string? NStringProperty { get; set; }
    public partial TestStruct Struct { get; set; }
    public partial TestStruct? NStruct { get; set; }
    public partial TestClass? Group { get; set; }
    public partial TestRecord? Record { get; set; }
    public partial TestRecordStruct? RecordStruct { get; set; }
    public partial char CharProperty { get; set; }
    public partial char? NCharProperty { get; set; }
    public partial int IntProperty { get; set; }
    public partial int? NIntProperty { get; set; }
    public partial uint UIntProperty { get; set; }
    public partial uint? NUIntProperty { get; set; }
    public partial short ShortProperty { get; set; }
    public partial short? NShortProperty { get; set; }
    public partial ushort UShortProperty { get; set; }
    public partial ushort? NUShortProperty { get; set; }
    public partial long LongProperty { get; set; }
    public partial long? NLongProperty { get; set; }
    public partial ulong ULongProperty { get; set; }
    public partial ulong? NULongProperty { get; set; }
    public partial sbyte SByteProperty { get; set; }
    public partial sbyte? NSByteProperty { get; set; }
    public partial byte ByteProperty { get; set; }
    public partial byte? NByteProperty { get; set; }
    public partial decimal DecimalProperty { get; set; }
    public partial decimal? NDecimalProperty { get; set; }
    public partial double DoubleProperty { get; set; }
    public partial double? NDoubleProperty { get; set; }
    public partial float FloatProperty { get; set; }
    public partial float? NFloatProperty { get; set; }
    public partial Guid GuidProperty { get; set; }
    public partial Guid? NGuidProperty { get; set; }
    public partial DateOnly DateOnlyProperty { get; set; }
    public partial DateOnly? NDateOnlyProperty { get; set; }
    public partial TimeOnly TimeOnlyProperty { get; set; }
    public partial TimeOnly? NTimeOnlyProperty { get; set; }
    public partial DateTime DateTimeProperty { get; set; }
    public partial DateTime? NDateTimeProperty { get; set; }
    public partial DateTimeOffset DateTimeOffsetProperty { get; set; }
    public partial DateTimeOffset? NDateTimeOffsetProperty { get; set; }
    public partial TimeSpan TimeSpanProperty { get; set; }
    public partial TimeSpan? NTimeSpanProperty { get; set; }
    public partial List<int> IntListProperty { get; set; }
    public partial List<int>? NIntListProperty { get; set; }
    public partial byte[] ByteArray { get; set; } = null!;
    public partial char[] CharArray { get; set; } = null!;
    public partial int[] IntArray { get; set; }
    public partial int[]? NIntArray { get; set; }

    public partial Int8BaseEnum Int8BaseEnumProperty { get; set; }
    public partial UInt8BaseEnum UInt8BaseEnumProperty { get; set; }
    public partial Int16BaseEnum Int16BaseEnumProperty { get; set; }
    public partial UInt16BaseEnum UInt16BaseEnumProperty { get; set; }
    public partial Int32BaseEnum Int32BaseEnumProperty { get; set; }
    public partial UInt32BaseEnum UInt32BaseEnumProperty { get; set; }
    public partial Int64BaseEnum Int64BaseEnumProperty { get; set; }
    public partial UInt64BaseEnum UInt64BaseEnumProperty { get; set; }

    public partial Int8BaseEnum? NInt8BaseEnumProperty { get; set; }
    public partial UInt8BaseEnum? NUInt8BaseEnumProperty { get; set; }
    public partial Int16BaseEnum? NInt16BaseEnumProperty { get; set; }
    public partial UInt16BaseEnum? NUInt16BaseEnumProperty { get; set; }
    public partial Int32BaseEnum? NInt32BaseEnumProperty { get; set; }
    public partial UInt32BaseEnum? NUInt32BaseEnumProperty { get; set; }
    public partial Int64BaseEnum? NInt64BaseEnumProperty { get; set; }
    public partial UInt64BaseEnum? NUInt64BaseEnumProperty { get; set; }

    public virtual partial string? VirtualProperty { get; set; }

}
