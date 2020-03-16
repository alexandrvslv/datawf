using DataWF.Common;
using System;

namespace DataWF.Test.Common
{
    public class TestExactFieldInvoker : Invoker<TestClass, int>
    {
        public TestExactFieldInvoker()
        {
        }

        public override bool CanWrite { get { return true; } }

        public override string Name => "Field";

        public override int GetValue(TestClass target)
        {
            return target.Field;
        }

        public override void SetValue(TestClass target, int value)
        {
            target.Field = value;
        }
    }
}
