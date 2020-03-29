using DataWF.Common;
using System;

namespace DataWF.Test.Common
{
    public class TestExactInlinePropertyInvoker : Invoker<TestClass, int>
    {
        public TestExactInlinePropertyInvoker()
        {
        }

        public override bool CanWrite { get { return true; } }

        public override string Name => "Group.Struct.Width";

        public override int GetValue(TestClass target)
        {
            return target.Group?.Struct.Width ?? 0;
        }

        public override void SetValue(TestClass target, int value)
        {
            var temp = target.Group;
            if (temp != null)
            {
                var g = temp.Struct;
                g.Width = value;
                temp.Struct = g;
            }
        }
    }
}
