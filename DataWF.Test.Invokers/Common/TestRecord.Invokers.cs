using DataWF.Invokers;

namespace DataWF.Test;

[Invoker(Instance = true)]
public partial record TestRecord(
    int IntProperty,
    int? NIntProperty,
    string StringProperty,
    int[] IntArrayProperty)
{
    public int Field;
}

[Invoker(Instance = true)]
public readonly partial record struct TestRecordStruct(
    int IntProperty,
    int? NIntProperty,
    string StringProperty,
    int[] IntArrayProperty)
{ }
