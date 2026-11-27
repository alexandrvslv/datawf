using DataWF.Serialization;
using MemoryPack;
using MessagePack;
using System.Runtime.Serialization;

namespace DataWF.Test;

[MemoryPackable]
[MessagePackObject(AllowPrivate = true)]
[ElementSerializer(Instance = true)]
[DataContract]
public partial record TestRecord(
    [property: Key(0), MemoryPackOrder(0), DataMember(Order = 0)] int IntProperty,
    [property: Key(1), MemoryPackOrder(1), DataMember(Order = 1)] int? NIntProperty,
    [property: Key(2), MemoryPackOrder(2), DataMember(Order = 2)] string StringProperty,
    [property: Key(3), MemoryPackOrder(3), DataMember(Order = 3)] int[] IntArrayProperty)
{
    [Key(4), MemoryPackOrder(4), DataMember(Order = 4)]
    public int Field;
}

[MemoryPackable]
[MessagePackObject(AllowPrivate = true)]
[ElementSerializer(Instance = true)]
[DataContract]
public readonly partial record struct TestRecordStruct(
    [property: Key(0), MemoryPackOrder(0), DataMember(Order = 0)] int IntProperty,
    [property: Key(1), MemoryPackOrder(1), DataMember(Order = 1)] int? NIntProperty,
    [property: Key(2), MemoryPackOrder(2), DataMember(Order = 2)] string StringProperty,
    [property: Key(3), MemoryPackOrder(3), DataMember(Order = 3)] int[] IntArrayProperty)
{
}
