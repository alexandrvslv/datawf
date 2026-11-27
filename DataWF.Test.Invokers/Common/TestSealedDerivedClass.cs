using DataWF.Invokers;

namespace DataWF.Test;

[Invoker()]
public sealed partial class TestSealedDerivedClass : TestDerivedClass
{
    public override string? VirtualProperty { get; set; }
    public int New2IntProperty { get; set; }
}

