using System;

namespace DataWF.Common
{
    public class ToStringInvoker : IInvoker<object, string>
    {
        public static readonly IInvoker<object, string> Instance = new ToStringInvoker();

        public bool CanWrite { get { return false; } }

        public Type DataType { get { return typeof(string); } set { } }

        public Type TargetType { get { return typeof(object); } }

        public string Name { get { return nameof(Object.ToString); } set { } }

        public string Get(object target)
        {
            return target?.ToString();
        }

        object IInvoker.Get(object target)
        {
            return Get(target);
        }

        public void Set(object target, object value)
        {
            throw new NotSupportedException();
        }

        public void Set(object target, string value)
        {
            throw new NotImplementedException();
        }

        public IListIndex CreateIndex()
        {
            return new ListIndex<object, string>(this);
        }
    }
}
