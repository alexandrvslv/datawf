using DataWF.Common;

namespace DataWF.Test.Common
{
    public class TestInlineIndexInvoker : Invoker<TestClass, int>
    {
        public TestInlineIndexInvoker()
        {
        }

        public override bool CanWrite => true;

        public override string Name => "Group.Item[1]";

        public override int GetValue(TestClass target) => target.Group?[1] ?? 0;

        public override void SetValue(TestClass target, int value)
        {
            var g = target.Group;
            if (g != null)
            {
                g[1] = value;
            }
        }
    }
}
