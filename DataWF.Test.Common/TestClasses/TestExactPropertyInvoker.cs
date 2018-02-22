using DataWF.Common;
using System;

namespace DataWF.Test.Common
{
    public class TestExactPropertyInvoker : IInvoker<TestClass, int>
    {
        public TestExactPropertyInvoker()
        {
            Name = "X";
            DataType = typeof(int);
        }

        public bool CanWrite { get { return true; } }

        public Type DataType { get; set; }

        public Type TargetType { get { return typeof(TestClass); } }

        public string Name { get; set; }

        public int Get(TestClass target)
        {
            return target.X;
        }

        public object Get(object target)
        {
            return Get((TestClass)target);
        }

        public void Set(TestClass target, int value)
        {
            target.X = value;
        }

        public void Set(object target, object value)
        {
            Set((TestClass)target, (int)value);
        }
    }
}
