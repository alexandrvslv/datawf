using DataWF.Common;
using System;

namespace DataWF.Test.Common
{
    public class TestExactFieldInvoker : IInvoker<TestClass, int>
    {
        public TestExactFieldInvoker()
        {
            Name = "Field";
            DataType = typeof(int);
        }

        public bool CanWrite { get { return true; } }

        public Type TargetType { get { return typeof(TestClass); } }

        public Type DataType { get; set; }

        public string Name { get; set; }

        public IListIndex CreateIndex()
        {
            return new ListIndex<TestClass, int>(this);
        }

        public int Get(TestClass target)
        {
            return target.Field;
        }

        public object Get(object target)
        {
            return Get((TestClass)target);
        }

        public void Set(TestClass target, int value)
        {
            target.Field = value;
        }

        public void Set(object target, object value)
        {
            Set((TestClass)target, (int)value);
        }
    }
}
