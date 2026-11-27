using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DataWF.Serialization;
using MemoryPack;
using MessagePack;

namespace DataWF.Test;

[ElementSerializer(Instance = true)]
[MemoryPackable]
[MessagePackObject]
[DataContract]
public partial class TestClass
{
    [MemoryPackOrder(0), DataMember(Order = 0)]
    [Key(0)]
    public int Field;

    [MemoryPackOrder(2), DataMember(Order = 2)]
    [Key(2)]
    public partial string StringProperty { get; set; }
    
    [MemoryPackOrder(3), DataMember(Order = 3)]
    [Key(3)]
    public partial string? NStringProperty { get; set; }
    
    [MemoryPackOrder(4), DataMember(Order = 4)]
    [Key(4)]
    public partial TestStruct Struct { get; set; }
    
    [MemoryPackOrder(5), DataMember(Order = 5)]
    [Key(5)]
    public partial TestStruct? NStruct { get; set; }
    
    [MemoryPackOrder(6), DataMember(Order = 6)]
    [Key(6)]
    public partial TestClass? Group { get; set; }

    [MemoryPackOrder(7), DataMember(Order = 7)]
    [Key(7)]
    public partial TestRecord? Record { get; set; }

    [MemoryPackOrder(67), DataMember(Order = 67)]
    [Key(67)]
    public partial TestRecordStruct? RecordStruct { get; set; }
    
    [MemoryPackOrder(8), DataMember(Order = 8)]
    [Key(8)]
    public partial char CharProperty { get; set; }
    
    [MemoryPackOrder(9), DataMember(Order = 9)]
    [Key(9)]
    public partial char? NCharProperty { get; set; }
    
    [MemoryPackOrder(10), DataMember(Order = 10)]
    [Key(10)]
    public partial int IntProperty { get; set; }
    
    [MemoryPackOrder(11), DataMember(Order = 11)]
    [Key(11)]
    public partial int? NIntProperty { get; set; }
    
    [MemoryPackOrder(12), DataMember(Order = 12)]
    [Key(12)]
    public partial uint UIntProperty { get; set; }
    
    [MemoryPackOrder(13), DataMember(Order = 13)]
    [Key(13)]
    public partial uint? NUIntProperty { get; set; }
    
    [MemoryPackOrder(14), DataMember(Order = 14)]
    [Key(14)]
    public partial short ShortProperty { get; set; }
    
    [MemoryPackOrder(15), DataMember(Order = 15)]
    [Key(15)]
    public partial short? NShortProperty { get; set; }
    
    [MemoryPackOrder(16), DataMember(Order = 16)]
    [Key(16)]
    public partial ushort UShortProperty { get; set; }
    
    [MemoryPackOrder(17), DataMember(Order = 17)]
    [Key(17)]
    public partial ushort? NUShortProperty { get; set; }
    
    [MemoryPackOrder(18), DataMember(Order = 18)]
    [Key(18)]
    public partial long LongProperty { get; set; }
    
    [MemoryPackOrder(19), DataMember(Order = 19)]
    [Key(19)]
    public partial long? NLongProperty { get; set; }
    
    [MemoryPackOrder(20), DataMember(Order = 20)]
    [Key(20)]
    public partial ulong ULongProperty { get; set; }
    
    [MemoryPackOrder(21), DataMember(Order = 21)]
    [Key(21)]
    public partial ulong? NULongProperty { get; set; }
    
    [MemoryPackOrder(22), DataMember(Order = 22)]
    [Key(22)]
    public partial sbyte SByteProperty { get; set; }
    
    [MemoryPackOrder(23), DataMember(Order = 23)]
    [Key(23)]
    public partial sbyte? NSByteProperty { get; set; }
    
    [MemoryPackOrder(24), DataMember(Order = 24)]
    [Key(24)]
    public partial byte ByteProperty { get; set; }
    
    [MemoryPackOrder(25), DataMember(Order = 25)]
    [Key(25)]
    public partial byte? NByteProperty { get; set; }
    
    [MemoryPackOrder(26), DataMember(Order = 26)]
    [Key(26)]
    public partial decimal DecimalProperty { get; set; }
    
    [MemoryPackOrder(27), DataMember(Order = 27)]
    [Key(27)]
    public partial decimal? NDecimalProperty { get; set; }
    
    [MemoryPackOrder(28), DataMember(Order = 28)]
    [Key(28)]
    public partial double DoubleProperty { get; set; }
    
    [MemoryPackOrder(29), DataMember(Order = 29)]
    [Key(29)]
    public partial double? NDoubleProperty { get; set; }
    
    [MemoryPackOrder(30), DataMember(Order = 30)]
    [Key(30)]
    public partial float FloatProperty { get; set; }
    
    [MemoryPackOrder(31), DataMember(Order = 31)]
    [Key(31)]
    public partial float? NFloatProperty { get; set; }
    
    [MemoryPackOrder(32), DataMember(Order = 32)]
    [Key(32)]
    public partial Guid GuidProperty { get; set; }
    
    [MemoryPackOrder(33), DataMember(Order = 33)]
    [Key(33)]
    public partial Guid? NGuidProperty { get; set; }
    
    [MemoryPackOrder(34), DataMember(Order = 34)]
    [Key(34)]
    public partial DateOnly DateOnlyProperty { get; set; }
    
    [MemoryPackOrder(35), DataMember(Order = 35)]
    [Key(35)]
    public partial DateOnly? NDateOnlyProperty { get; set; }
    
    [MemoryPackOrder(36), DataMember(Order = 36)]
    [Key(36)]
    public partial TimeOnly TimeOnlyProperty { get; set; }
    
    [MemoryPackOrder(37), DataMember(Order = 37)]
    [Key(37)]
    public partial TimeOnly? NTimeOnlyProperty { get; set; }
    
    [MemoryPackOrder(38), DataMember(Order = 38)]
    [Key(38)]
    public partial DateTime DateTimeProperty { get; set; }
    
    [MemoryPackOrder(39), DataMember(Order = 39)]
    [Key(39)]
    public partial DateTime? NDateTimeProperty { get; set; }
    
    [MemoryPackOrder(40), DataMember(Order = 40)]
    [Key(40)]
    public partial DateTimeOffset DateTimeOffsetProperty { get; set; }
    
    [MemoryPackOrder(41), DataMember(Order = 41)]
    [Key(41)]
    public partial DateTimeOffset? NDateTimeOffsetProperty { get; set; }
    
    [MemoryPackOrder(42), DataMember(Order = 42)]
    [Key(42)]
    public partial TimeSpan TimeSpanProperty { get; set; }
    
    [MemoryPackOrder(43), DataMember(Order = 43)]
    [Key(43)]
    public partial TimeSpan? NTimeSpanProperty { get; set; }
    
    [MemoryPackOrder(44), DataMember(Order = 44)]
    [Key(44)]
    public partial List<int> IntListProperty { get; set; }
    
    [MemoryPackOrder(45), DataMember(Order = 45)]
    [Key(45)]
    public partial List<int>? NIntListProperty { get; set; }
    
    [MemoryPackOrder(46), DataMember(Order = 46)]
    [Key(46)]
    public partial byte[] ByteArray { get; set; } = null!;
    
    [MemoryPackOrder(47), DataMember(Order = 47)]
    [Key(47)]
    public partial char[] CharArray { get; set; } = null!;
    
    [MemoryPackOrder(48), DataMember(Order = 48)]
    [Key(48)]
    public partial int[] IntArray { get; set; }
    
    [MemoryPackOrder(49), DataMember(Order = 49)]
    [Key(49)]
    public partial int[]? NIntArray { get; set; }

    [MemoryPackOrder(50), DataMember(Order = 50)]
    [Key(50)]
    public partial Int8BaseEnum Int8BaseEnumProperty { get; set; }

    [MemoryPackOrder(51), DataMember(Order = 51)]
    [Key(51)]
    public partial UInt8BaseEnum UInt8BaseEnumProperty { get; set; }

    [MemoryPackOrder(52), DataMember(Order = 52)]
    [Key(52)]
    public partial Int16BaseEnum Int16BaseEnumProperty { get; set; }

    [MemoryPackOrder(53), DataMember(Order = 53)]
    [Key(53)]
    public partial UInt16BaseEnum UInt16BaseEnumProperty { get; set; }

    [MemoryPackOrder(54), DataMember(Order = 54)]
    [Key(54)]
    public partial Int32BaseEnum Int32BaseEnumProperty { get; set; }

    [MemoryPackOrder(55), DataMember(Order = 55)]
    [Key(55)]
    public partial UInt32BaseEnum UInt32BaseEnumProperty { get; set; }

    [MemoryPackOrder(56), DataMember(Order = 56)]
    [Key(56)]
    public partial Int64BaseEnum Int64BaseEnumProperty { get; set; }

    [MemoryPackOrder(57), DataMember(Order = 57)]
    [Key(57)]
    public partial UInt64BaseEnum UInt64BaseEnumProperty { get; set; }

    [MemoryPackOrder(58), DataMember(Order = 58)]
    [Key(58)]
    public partial Int8BaseEnum? NInt8BaseEnumProperty { get; set; }

    [MemoryPackOrder(59), DataMember(Order = 59)]
    [Key(59)]
    public partial UInt8BaseEnum? NUInt8BaseEnumProperty { get; set; }

    [MemoryPackOrder(60), DataMember(Order = 60)]
    [Key(60)]
    public partial Int16BaseEnum? NInt16BaseEnumProperty { get; set; }

    [MemoryPackOrder(61), DataMember(Order = 61)]
    [Key(61)]
    public partial UInt16BaseEnum? NUInt16BaseEnumProperty { get; set; }

    [MemoryPackOrder(62), DataMember(Order = 62)]
    [Key(62)]
    public partial Int32BaseEnum? NInt32BaseEnumProperty { get; set; }

    [MemoryPackOrder(63), DataMember(Order = 63)]
    [Key(63)]
    public partial UInt32BaseEnum? NUInt32BaseEnumProperty { get; set; }

    [MemoryPackOrder(64), DataMember(Order = 64)]
    [Key(64)]
    public partial Int64BaseEnum? NInt64BaseEnumProperty { get; set; }

    [MemoryPackOrder(65), DataMember(Order = 65)]
    [Key(65)]
    public partial UInt64BaseEnum? NUInt64BaseEnumProperty { get; set; }

    [MemoryPackOrder(66), DataMember(Order = 66)]
    [Key(66)]
    public virtual partial string? VirtualProperty { get; set; }
    
    
}
