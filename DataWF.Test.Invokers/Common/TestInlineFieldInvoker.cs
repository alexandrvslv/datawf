using DataWF.Invokers;

namespace DataWF.Test.Invokers;

public class TestInlineFieldInvoker : Invoker<TestClass, int>
{
    public override bool CanWrite => true;

    public override string Name => "Group.Struct.Field";

    public override int GetValue(scoped in TestClass target) => target.Group?.Struct.Field ?? 0;

    public override void SetValue(scoped ref TestClass target, int value)
    {
        var g = target.Group;
        if (g != null)
        {
            var s = g.Struct;
            s.Field = value;
            g.Struct = s;
        }
    }
}
