using DataWF.Common;
using System;

namespace DataWF.Test.Common
{
    public class TestExactInlineFieldInvoker : Invoker<TestClass, int>
    {
        public TestExactInlineFieldInvoker()
        {
        }

        public override bool CanWrite => true;

        public override string Name => "Struct.Field";

        public override int GetValue(TestClass target)
        {
            var g = target.Struct;
            return g.Field;
        }

        public override void SetValue(TestClass target, int value)
        {
            var g = target.Struct;
            g.Field = value;
            target.Struct = g;
        }
    }
}
