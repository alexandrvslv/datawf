using DataWF.Invokers;

namespace DataWF.Test;

[Invoker(Instance = true)]
public partial struct TestStruct
{
    public int Field;
    public partial int IntProperty { get; set; }
    public partial int? NIntProperty { get; set; }
}