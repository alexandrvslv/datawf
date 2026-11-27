using DataWF.Serialization;
using MemoryPack;
using MessagePack;
using System.Runtime.Serialization;

namespace DataWF.Test;

[ElementSerializer(Instance = true)]
[MemoryPackable]
[MessagePackObject]
[DataContract]
public partial struct TestStruct
{
    [MemoryPackOrder(0), DataMember(Order = 0)]
    [Key(0)]
    public int Field;

    [MemoryPackOrder(1), DataMember(Order = 1)]
    [Key(1)]
    public partial int IntProperty { get; set; }

    [MemoryPackOrder(2), DataMember(Order = 2)]
    [Key(2)]
    public partial int? NIntProperty { get; set; }
}
