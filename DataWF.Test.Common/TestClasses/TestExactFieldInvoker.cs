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

        public IListIndex CreateIndex(bool concurrent)
        {
            return new ListIndex<TestClass, int>(this, int.MinValue);
        }

        public int GetValue(TestClass target)
        {
            return target.Field;
        }

        public object GetValue(object target)
        {
            return GetValue((TestClass)target);
        }

        public void SetValue(TestClass target, int value)
        {
            target.Field = value;
        }

        public void SetValue(object target, object value)
        {
            SetValue((TestClass)target, (int)value);
        }
    }
}
