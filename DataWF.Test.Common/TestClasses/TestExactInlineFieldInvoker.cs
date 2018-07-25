using DataWF.Common;
using System;

namespace DataWF.Test.Common
{
    public class TestExactInlineFieldInvoker : IInvoker<TestClass, int>
    {
        public TestExactInlineFieldInvoker()
        {
            Name = "Struct.Field";
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
            var g = target.Struct;
            return g.Field;
        }

        public object GetValue(object target)
        {
            return GetValue((TestClass)target);
        }

        public void SetValue(TestClass target, int value)
        {
            var g = target.Struct;
            g.Field = value;
            target.Struct = g;
        }

        public void SetValue(object target, object value)
        {
            SetValue((TestClass)target, (int)value);
        }
    }
}
