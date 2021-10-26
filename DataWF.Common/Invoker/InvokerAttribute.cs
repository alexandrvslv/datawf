using System;

namespace DataWF.Common
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class InvokerAttribute : Attribute
    {
        public InvokerAttribute(Type targetType, string propertyName, Type invokerType)
        {
            TargetType = targetType;
            PropertyName = propertyName;
            InvokerType = invokerType;
        }

        public Type TargetType { get; }
        public Type InvokerType { get; }
        public string PropertyName { get; }
    }
}
