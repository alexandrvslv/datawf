using DataWF.Common;
using System;

namespace DataWF.Test.Common
{
    public class TestPropertyInvoker : Invoker<TestClass, int>
    {
        public TestPropertyInvoker()
        { }

        public override bool CanWrite => true;

        public override string Name => "X";

        public override int GetValue(TestClass target) => target.X;

        public override void SetValue(TestClass target, int value) => target.X = value;
    }
}
