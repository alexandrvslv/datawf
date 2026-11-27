using DataWF.Invokers;

namespace DataWF.Test.Invokers;

public class TestIntPropertyInvoker : Invoker<TestClass, int>
{
    public static readonly TestIntPropertyInvoker Instance = new();

    public override bool CanWrite => true;

    public override string Name => nameof(TestClass.IntProperty);

    public override int GetValue(scoped in TestClass target) => target.IntProperty;

    public override void SetValue(scoped ref TestClass target, int value) => target.IntProperty = value;
}

