using Portable.Xaml.Markup;
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
            DataType = Property.PropertyType;
            IsText = TypeHelper.IsXmlText(property);
            IsAttribute = TypeHelper.IsXmlAttribute(property) && !IsText;
            Invoker = EmitInvoker.Initialize(property, true);
            Default = TypeHelper.GetDefault(property);
            if (IsAttribute || IsText)
            {
                Serialazer = TypeHelper.GetValueSerializer(property);
            }
            Name = property.Name;
        }

        public IInvoker Invoker { get; }

        public PropertyInfo Property { get; }

        public string Name { get; set; }

        public Type DataType { get; set; }

        public bool IsAttribute { get; }

        public bool IsText { get; }

        public object Default { get; }

        public ValueSerializer Serialazer { get; }

        public bool CheckDefault(object value)
        {
            if (Default == null)
                return value == null;
            return Default.Equals(value);
        }

        public string TextFormat(object value)
        {
            return Serialazer != null
                ? Serialazer.ConvertToString(value, null)
                : Helper.TextBinaryFormat(value);
        }

        public object TextParse(string value)
        {
            return Serialazer != null
                ? Serialazer.ConvertFromString(value, null)
                : Helper.TextParse(value, DataType);
        }
    }
}
