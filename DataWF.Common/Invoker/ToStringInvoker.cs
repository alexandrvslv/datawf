using System;

namespace DataWF.Common
{
    public class ToStringInvoker : IInvoker
    {
        public bool CanWrite { get { return false; } }

        public Type DataType { get { return typeof(string); } set{} }

        public string Name { get { return nameof(Object.ToString); } set{} }

        public object Get(object target)
        {
            return target?.ToString();
        }

        public void Set(object target, object value)
        {
            throw new NotSupportedException();
        }
    }
}
