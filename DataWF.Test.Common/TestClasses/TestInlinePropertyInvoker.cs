using DataWF.Common;
using System;

namespace DataWF.Test.Common
{
    public class TestInlinePropertyInvoker : Invoker<TestClass, int>
    {
        public TestInlinePropertyInvoker()
        {
        }

        public override bool CanWrite => true;

        public override string Name => "Group.Struct.Width";

        public override int GetValue(TestClass target) => target.Group?.Struct.Width ?? 0;

        public override void SetValue(TestClass target, int value)
        {
            var g = target.Group;
            if (g != null)
            {
                var s = g.Struct;
                s.Width = value;
                g.Struct = s;
            }
        }
    }
}
