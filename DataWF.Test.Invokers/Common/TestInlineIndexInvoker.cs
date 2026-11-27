using DataWF.Invokers;

namespace DataWF.Test.Invokers;

public class TestInlineIndexInvoker : Invoker<TestClass, int>
{
    public override bool CanWrite => true;

    public override string Name => "Group.Item[1]";

    public override int GetValue(scoped in TestClass target) => target.Group?[1] ?? 0;

    public override void SetValue(scoped ref TestClass target, int value)
    {
        var g = target.Group;
        if (g != null)
        {
            g[1] = value;
        }
    }
}
