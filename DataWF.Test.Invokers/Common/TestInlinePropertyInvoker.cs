using DataWF.Invokers;

namespace DataWF.Test.Invokers;

public class TestInlinePropertyInvoker : Invoker<TestClass, int>
{
    public override bool CanWrite => true;

    public override string Name => "Group.Struct.IntProeprty";

    public override int GetValue(scoped in TestClass target) => target.Group?.Struct.IntProperty ?? 0;

    public override void SetValue(scoped ref TestClass target, int value)
    {
        var g = target.Group;
        if (g != null)
        {
            var s = g.Struct;
            s.IntProperty = value;
            g.Struct = s;
        }
    }
}
