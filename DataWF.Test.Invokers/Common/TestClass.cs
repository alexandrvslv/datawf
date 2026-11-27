using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Unicode;
using DataWF.Invokers;

namespace DataWF.Test;

public partial class TestClass : IComparable<TestClass>, IEquatable<TestClass>
{
    public static List<TestClass> GenerateList(int cout = 5)
    {
        var items = new List<TestClass>();
        for (int i = 0; i < cout; i++)
        {
            items.Add(GenerateTestClass(i));
        }
        return items;
    }

    public static TestClass GenerateTestClass(int i) => new TestClass
    {
        Field = i,
        //CharProperty = ()
        LongProperty = i,
        NLongProperty = i,
        ULongProperty = (ulong)i,
        NULongProperty = (ulong)i,
        IntProperty = i,
        NIntProperty = i,
        UIntProperty = (uint)i,
        NUIntProperty = (uint)i,
        ShortProperty = unchecked((short)(i % 20)),
        NShortProperty = unchecked((short)i),
        UShortProperty = unchecked((ushort)i),
        NUShortProperty = unchecked((ushort)i),
        SByteProperty = unchecked((sbyte)i),
        NSByteProperty = unchecked((sbyte)i),
        ByteProperty = unchecked((byte)i),
        NByteProperty = unchecked((byte)i),
        DecimalProperty = i / 3.3M,
        NDecimalProperty = i / 3.3M,
        DoubleProperty = i / 3.3D,
        NDoubleProperty = i / 3.3D,
        FloatProperty = i / 3.3F,
        NFloatProperty = i / 3.3F,
        DateTimeProperty = DateTime.Now.AddHours(i),
        DateTimeOffsetProperty = DateTimeOffset.Now.AddHours(i),
        DateOnlyProperty = DateOnly.FromDateTime(DateTime.Now.AddHours(i)),
        TimeOnlyProperty = TimeOnly.FromDateTime(DateTime.Now.AddHours(i)),
        TimeSpanProperty = DateTime.Now.TimeOfDay.Add(TimeSpan.FromDays(i)),
        GuidProperty = Guid.NewGuid(),
        StringProperty = "bla bla " + i,
        Struct = new()
        {
            Field = i,
            IntProperty = i,
            NIntProperty = i,
        },
        ByteArray = UTF8Encoding.UTF8.GetBytes(i + "bla bla bla bla bla bla bla"),
        CharArray = [.. (i + "bla bla bla bla bla bla bla")],
        IntArray = [1, i, 3],
        IntListProperty = [1, i, 3],
        Group = new()
        {
            IntProperty = i,
            NIntProperty = i,
            DecimalProperty = i / 25.25M,
            StringProperty = "bla bla bla",
            IntArray = [1, i, 3],
            Struct = new()
            {
                Field = i,
                IntProperty = i,
                NIntProperty = i,
            },
            VirtualProperty = "Generated index for group : " + i
        },
        Record = new(i, i, "bla bla " + i, [1, i, 3]) { Field = i },
        RecordStruct = new(i, i, "bla bla " + i, [1, i, 3]),
        Int8BaseEnumProperty = (Int8BaseEnum)(i % 3),
        UInt8BaseEnumProperty = (UInt8BaseEnum)(i % 3),
        Int16BaseEnumProperty = (Int16BaseEnum)(i % 3),
        UInt16BaseEnumProperty = (UInt16BaseEnum)(i % 3),
        Int32BaseEnumProperty = (Int32BaseEnum)(i % 3),
        UInt32BaseEnumProperty = (UInt32BaseEnum)(i % 3),
        Int64BaseEnumProperty = (Int64BaseEnum)(i % 3),
        UInt64BaseEnumProperty = (UInt64BaseEnum)(i % 3),
        VirtualProperty = "Generated index : " + i
    };

    public int CompareTo(TestClass? other) => UIntProperty.CompareTo(UIntProperty);

    public bool Equals(TestClass? other) => UIntProperty == other?.UIntProperty;

    public override bool Equals(object? obj) => Equals(obj as TestClass);

    public override int GetHashCode() => (int)UIntProperty;

    public override string ToString() => UIntProperty.ToString();

    public partial string StringProperty { get => field; set => field = value; } = string.Empty;
    public partial string? NStringProperty { get => field; set => field = value; }
    public partial TestStruct Struct { get => field; set => field = value; }
    public partial TestStruct? NStruct { get => field; set => field = value; }
    public partial TestClass? Group { get => field; set => field = value; } = null!;
    public partial TestRecord? Record { get => field; set => field = value; }
    public partial TestRecordStruct? RecordStruct { get => field; set => field = value; }
    public partial char CharProperty { get => field; set => field = value; }
    public partial char? NCharProperty { get => field; set => field = value; }
    public partial int IntProperty { get => field; set => field = value; }
    public partial int? NIntProperty { get => field; set => field = value; }
    public partial uint UIntProperty { get => field; set => field = value; } = 1;
    public partial uint? NUIntProperty { get => field; set => field = value; }
    public partial short ShortProperty { get => field; set => field = value; }
    public partial short? NShortProperty { get => field; set => field = value; }
    public partial ushort UShortProperty { get => field; set => field = value; }
    public partial ushort? NUShortProperty { get => field; set => field = value; }
    public partial long LongProperty { get => field; set => field = value; }
    public partial long? NLongProperty { get => field; set => field = value; }
    public partial ulong ULongProperty { get => field; set => field = value; }
    public partial ulong? NULongProperty { get => field; set => field = value; }
    public partial sbyte SByteProperty { get => field; set => field = value; }
    public partial sbyte? NSByteProperty { get => field; set => field = value; }
    public partial byte ByteProperty { get => field; set => field = value; }
    public partial byte? NByteProperty { get => field; set => field = value; }
    public partial decimal DecimalProperty { get => field; set => field = value; }
    public partial decimal? NDecimalProperty { get => field; set => field = value; }
    public partial double DoubleProperty { get => field; set => field = value; }
    public partial double? NDoubleProperty { get => field; set => field = value; }
    public partial float FloatProperty { get => field; set => field = value; }
    public partial float? NFloatProperty { get => field; set => field = value; }
    public partial Guid GuidProperty { get => field; set => field = value; }
    public partial Guid? NGuidProperty { get => field; set => field = value; }
    public partial DateOnly DateOnlyProperty { get => field; set => field = value; }
    public partial DateOnly? NDateOnlyProperty { get => field; set => field = value; }
    public partial TimeOnly TimeOnlyProperty { get => field; set => field = value; }
    public partial TimeOnly? NTimeOnlyProperty { get => field; set => field = value; }
    public partial DateTime DateTimeProperty { get => field; set => field = value; }
    public partial DateTime? NDateTimeProperty { get => field; set => field = value; }
    public partial DateTimeOffset DateTimeOffsetProperty { get => field; set => field = value; }
    public partial DateTimeOffset? NDateTimeOffsetProperty { get => field; set => field = value; }
    public partial TimeSpan TimeSpanProperty { get => field; set => field = value; }
    public partial TimeSpan? NTimeSpanProperty { get => field; set => field = value; }
    public partial List<int> IntListProperty { get => field; set => field = value; } = [1, 2, 3];
    public partial List<int>? NIntListProperty { get => field; set => field = value; }
    public partial byte[] ByteArray { get => field; set => field = value; }
    public partial char[] CharArray { get => field; set => field = value; }
    public partial int[] IntArray { get => field; set => field = value; } = [1, 2, 3];
    public partial int[]? NIntArray { get => field; set => field = value; }

    public int this[int index]
    {
        get => IntArray[index];
        set => IntArray[index] = value;
    }

    public partial Int8BaseEnum Int8BaseEnumProperty { get => field; set => field = value; }
    public partial UInt8BaseEnum UInt8BaseEnumProperty { get => field; set => field = value; }
    public partial Int16BaseEnum Int16BaseEnumProperty { get => field; set => field = value; }
    public partial UInt16BaseEnum UInt16BaseEnumProperty { get => field; set => field = value; }
    public partial Int32BaseEnum Int32BaseEnumProperty { get => field; set => field = value; }
    public partial UInt32BaseEnum UInt32BaseEnumProperty { get => field; set => field = value; }
    public partial Int64BaseEnum Int64BaseEnumProperty { get => field; set => field = value; }
    public partial UInt64BaseEnum UInt64BaseEnumProperty { get => field; set => field = value; }

    public partial Int8BaseEnum? NInt8BaseEnumProperty { get => field; set => field = value; }
    public partial UInt8BaseEnum? NUInt8BaseEnumProperty { get => field; set => field = value; }
    public partial Int16BaseEnum? NInt16BaseEnumProperty { get => field; set => field = value; }
    public partial UInt16BaseEnum? NUInt16BaseEnumProperty { get => field; set => field = value; }
    public partial Int32BaseEnum? NInt32BaseEnumProperty { get => field; set => field = value; }
    public partial UInt32BaseEnum? NUInt32BaseEnumProperty { get => field; set => field = value; }
    public partial Int64BaseEnum? NInt64BaseEnumProperty { get => field; set => field = value; }
    public partial UInt64BaseEnum? NUInt64BaseEnumProperty { get => field; set => field = value; }

    public virtual partial string? VirtualProperty { get => field; set => field = value; }
}

public enum Int8BaseEnum : sbyte
{
    A, B, C
}

public enum UInt8BaseEnum : byte
{
    A, B, C
}

public enum Int16BaseEnum : short
{
    A, B, C
}

public enum UInt16BaseEnum : ushort
{
    A, B, C
}

public enum Int32BaseEnum : int
{
    A, B, C
}

public enum UInt32BaseEnum : uint
{
    A, B, C
}

public enum Int64BaseEnum : long
{
    A, B, C
}

public enum UInt64BaseEnum : ulong
{
    A, B, C
}

