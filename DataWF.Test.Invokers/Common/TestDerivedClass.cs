using DataWF.Invokers;

namespace DataWF.Test;

public partial class TestDerivedClass : TestClass
{
    public override string? VirtualProperty { get; set; }
    public int NewIntProperty { get; set; }
}

