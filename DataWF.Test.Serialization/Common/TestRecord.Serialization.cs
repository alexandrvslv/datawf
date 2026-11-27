using DataWF.Serialization;

namespace DataWF.Test;

[ElementSerializer(Instance = true)]
public partial record TestRecord(
    int IntProperty,
    int? NIntProperty,
    string StringProperty,
    int[] IntArrayProperty)
{
    public int Field;
}

[ElementSerializer(Instance = true)]
public readonly partial record struct TestRecordStruct(
    int IntProperty,
    int? NIntProperty,
    string StringProperty,
    int[] IntArrayProperty)
{}

