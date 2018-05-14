using System;
using System.ComponentModel;
using System.Reflection;

namespace DataWF.Common
{
    public class PropertySerializationInfo
    {
        public PropertySerializationInfo(PropertyInfo property)
        {
            Property = property;
            IsAttribute = TypeHelper.IsXmlAttribute(property);
            IsText = TypeHelper.IsXmlText(property);
            Default = TypeHelper.GetDefault(property);
            Invoker = EmitInvoker.Initialize(property);
            PropertyName = property.Name;
        }

        public IInvoker Invoker { get; private set; }

        public PropertyInfo Property { get; private set; }

        public string PropertyName { get; private set; }

        public Type PropertyType { get { return Property.PropertyType; } }

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
