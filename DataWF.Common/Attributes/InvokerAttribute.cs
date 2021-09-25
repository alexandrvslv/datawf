using System;

namespace DataWF.Common
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class InvokerAttribute : Attribute
    {
        public InvokerAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }

        public InvokerAttribute(Type targetType, string propertyName) : this(propertyName)
        {
            TargetType = targetType;
        }

        public string PropertyName { get; }

        public Type TargetType { get; }
    }
}
