using DataWF.Common;
using System;

namespace DataWF.Test.Common
{
    public class TestInlineFieldInvoker : Invoker<TestClass, int>
    {
        public TestInlineFieldInvoker()
        {
        }

        public override bool CanWrite => true;

        public override string Name => "Group.Struct.Field";

        public override int GetValue(TestClass target) => target.Group?.Struct.Field ?? 0;

        public override void SetValue(TestClass target, int value)
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
}
