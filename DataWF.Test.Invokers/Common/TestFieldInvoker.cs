using DataWF.Invokers;

namespace DataWF.Test.Invokers;

public class TestFieldInvoker : Invoker<TestClass, int>
{
    public override bool CanWrite => true;

    public override string Name => nameof(TestClass.Field);

    public override int GetValue(scoped in TestClass target) => target.Field;

    public override void SetValue(scoped ref TestClass target, int value) => target.Field = value;
}
