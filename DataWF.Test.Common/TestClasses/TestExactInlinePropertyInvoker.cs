using DataWF.Common;
using System;

namespace DataWF.Test.Common
{
    public class TestExactInlinePropertyInvoker : IInvoker<TestClass, int>
    {
        public TestExactInlinePropertyInvoker()
        {
            Name = "Group.Struct.Width";
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
            return target.Group?.Struct.Width ?? 0;
        }

        public object Get(object target)
        {
            return Get((TestClass)target);
        }

        public void Set(TestClass target, int value)
        {
            var temp = target.Group;
            if (temp != null)
            {
                var g = temp.Struct;
                g.Width = value;
                temp.Struct = g;
            }
        }

        public void Set(object target, object value)
        {
            Set((TestClass)target, (int)value);
        }
    }
}
