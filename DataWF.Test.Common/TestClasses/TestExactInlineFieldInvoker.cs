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

        public string Name { get; set; }

        public int Get(TestClass target)
        {
            var g = target.Struct;
            return g.Field;
        }

        public object Get(object target)
        {
            return Get((TestClass)target);
        }

        public void Set(TestClass target, int value)
        {
            var g = target.Struct;
            g.Field = value;
            target.Struct = g;
        }

        public void Set(object target, object value)
        {
            Set((TestClass)target, (int)value);
        }
    }
}
