using DataWF.Invokers;

namespace DataWF.Test.Invokers;

public class TestStructIntPropertyInvoker : Invoker<TestStruct, int>, IValueTypeInvoker<TestStruct, int>
{
    public static readonly TestStructIntPropertyInvoker Instance = new();

    public override bool CanWrite => true;

    public override string Name => nameof(TestStruct.IntProperty);

    public override int GetValue(scoped in TestStruct target) => target.IntProperty;

    public override void SetValue(scoped ref TestStruct target, int value) => target.IntProperty = value;
}
