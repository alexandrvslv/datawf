using DataWF.Common;
using System;

namespace DataWF.Test.Common
{
    public class TestExactPropertyInvoker : Invoker<TestClass, int>
    {
        public TestExactPropertyInvoker()
        {
        }

        public override bool CanWrite { get { return true; } }

        public override string Name => "X";

        public override int GetValue(TestClass target)
        {
            return target.X;
        }

        public override void SetValue(TestClass target, int value)
        {
            target.X = value;
        }
    }
}
