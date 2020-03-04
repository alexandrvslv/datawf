using System;

namespace DataWF.Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public class InvokerAttribute : Attribute
    {
        public InvokerAttribute(Type targetType, string propertyName)
        {
            TargetType = targetType;
            PropertyName = propertyName;
        }

        public Type TargetType { get; }        
        public string PropertyName { get; }
    }
}
