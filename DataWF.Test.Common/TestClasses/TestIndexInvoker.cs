using DataWF.Common;

namespace DataWF.Test.Common
{
    public class TestIndexInvoker : IndexInvoker<TestClass, int, int>
    {
        private int index;

        public TestIndexInvoker(int index)
        {
            Index = index;
        }

        public override bool CanWrite => true;

        public override string Name => "X";

        public override int GetValue(TestClass target, int index) => target[index];

        public override void SetValue(TestClass target, int index, int value) => target[index] = value;

    }

}
