using Portable.Xaml.Markup;
using System;
using System.Reflection;

namespace DataWF.Common
{
    [Flags]
    public enum PropertySerializationInfoKeys
    {
        None,
        Attribute = 1,
        Text = 2,
        Writeable = 4,
        Required = 8,
        ChangeSensitive = 16,
    }

    public class PropertySerializationInfo : INamed
    {
        public PropertySerializationInfo()
        { }

        public PropertySerializationInfo(PropertyInfo property, int order = -1)
        {
            Property = property;
            DataType = Property.PropertyType;
            var keys = PropertySerializationInfoKeys.None;
            if (TypeHelper.IsSerializeText(property))
                keys |= PropertySerializationInfoKeys.Text;
            else if (TypeHelper.IsSerializeAttribute(property))
                keys |= PropertySerializationInfoKeys.Attribute;
            if (TypeHelper.IsSerializeWriteable(property))
                keys |= PropertySerializationInfoKeys.Writeable;
            if (TypeHelper.IsRequired(property))
                keys |= PropertySerializationInfoKeys.Required;
            else if (TypeHelper.IsJsonPropertyNullValueHandling(property) != null)
                keys |= PropertySerializationInfoKeys.ChangeSensitive;
            Keys = keys;
            Order = TypeHelper.GetOrder(property, order);
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

        public PropertySerializationInfoKeys Keys { get; }

        public bool IsAttribute => (Keys & PropertySerializationInfoKeys.Attribute) == PropertySerializationInfoKeys.Attribute;

        public bool IsChangeSensitive => (Keys & PropertySerializationInfoKeys.ChangeSensitive) == PropertySerializationInfoKeys.ChangeSensitive;

        public bool IsRequired => (Keys & PropertySerializationInfoKeys.Required) == PropertySerializationInfoKeys.Required;

        public bool IsText => (Keys & PropertySerializationInfoKeys.Text) == PropertySerializationInfoKeys.Text;

        public bool IsWriteable => (Keys & PropertySerializationInfoKeys.Writeable) == PropertySerializationInfoKeys.Writeable;

        public int Order { get; set; }

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

        public override string ToString()
        {
            return $"{Name} {DataType.Name} {Keys}";
        }
    }
}
