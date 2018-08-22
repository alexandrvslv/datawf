using System;
using System.Reflection;

namespace DataWF.Common
{
    public class PropertySerializationInfo : INamed
    {
        public PropertySerializationInfo()
        { }

        public PropertySerializationInfo(PropertyInfo property)
        {
            Property = property;
            IsText = TypeHelper.IsXmlText(property);
            IsAttribute = TypeHelper.IsXmlAttribute(property) && !IsText;
            Default = TypeHelper.GetDefault(property);
            Invoker = EmitInvoker.Initialize(property);
            Name = property.Name;
        }

        public IInvoker Invoker { get; private set; }

        public PropertyInfo Property { get; private set; }

        public string Name { get; set; }

        public Type DataType { get { return Property.PropertyType; } }

        public bool IsAttribute { get; private set; }

        public bool IsText { get; private set; }

        public object Default { get; private set; }

        public bool CheckDefault(object value)
        {
            if (Default == null && value == null)
                return true;
            if (Default == null)
                return false;
            return Default.Equals(value);
        }
    }
}
