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

        public IListIndex CreateIndex()
        {
            return new ListIndex<TestClass, int>(this);
        }

        public int GetValue(TestClass target)
        {
            return target.X;
        }

        public object GetValue(object target)
        {
            return GetValue((TestClass)target);
        }

        public void SetValue(TestClass target, int value)
        {
            target.X = value;
        }

        public void SetValue(object target, object value)
        {
            SetValue((TestClass)target, (int)value);
        }
    }
}
