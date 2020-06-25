using DataWF.Common;
using System;

namespace DataWF.Test.Common
{
    public class TestFieldInvoker : Invoker<TestClass, int>
    {
        public TestFieldInvoker()
        {
        }

        public override bool CanWrite => true;

        public override string Name => "Field";

        public override int GetValue(TestClass target) => target.Field;

        public override void SetValue(TestClass target, int value) => target.Field = value;
    }
}
