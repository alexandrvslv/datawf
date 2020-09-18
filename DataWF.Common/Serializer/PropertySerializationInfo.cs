using DataWF.Common;
using Portable.Xaml.Markup;
using System;
using System.Diagnostics;
using System.Reflection;

[assembly: Invoker(typeof(PropertySerializationInfo), nameof(PropertySerializationInfo.IsAttribute), typeof(PropertySerializationInfo.IsAttributeInvoker))]
[assembly: Invoker(typeof(PropertySerializationInfo), nameof(PropertySerializationInfo.Order), typeof(PropertySerializationInfo.OrderInvoker))]
[assembly: Invoker(typeof(PropertySerializationInfo), nameof(PropertySerializationInfo.Name), typeof(PropertySerializationInfo.NameInvoker))]
namespace DataWF.Common
{
    public class PropertySerializationInfo : INamed
    {
        public PropertySerializationInfo()
        { }

        public PropertySerializationInfo(PropertyInfo property, int order = -1)
        {
            Property = property;
            Name = property.Name;
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
            if (TypeHelper.IsJsonSynchronized(property))
                keys |= PropertySerializationInfoKeys.ChangeSensitive;
            if (TypeHelper.IsReadOnly(property))
                keys |= PropertySerializationInfoKeys.ReadOnly;
            Keys = keys;
            Order = TypeHelper.GetOrder(property, order);
            Invoker = EmitInvoker.Initialize(property, true);

            Default = TypeHelper.GetDefault(property);
            if (IsAttribute || IsText)
            {
                Serialazer = TypeHelper.GetValueSerializer(property);
            }
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

        public bool IsReadOnly => (Keys & PropertySerializationInfoKeys.ReadOnly) == PropertySerializationInfoKeys.ReadOnly;

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
            return $"{Order} {Name} {DataType.Name} {Keys}";
        }

        public class IsAttributeInvoker : Invoker<PropertySerializationInfo, bool>
        {
            public static readonly IsAttributeInvoker Instance = new IsAttributeInvoker();

            public override string Name => nameof(IsAttribute);

            public override bool CanWrite => false;

            public override bool GetValue(PropertySerializationInfo target) => target.IsAttribute;

            public override void SetValue(PropertySerializationInfo target, bool value) { }
        }

        public class OrderInvoker : Invoker<PropertySerializationInfo, int>
        {
            public static readonly OrderInvoker Instance = new OrderInvoker();

            public override string Name => nameof(Order);

            public override bool CanWrite => false;

            public override int GetValue(PropertySerializationInfo target) => target.Order;

            public override void SetValue(PropertySerializationInfo target, int value) => target.Order = value;
        }

        public class NameInvoker : Invoker<PropertySerializationInfo, string>
        {
            public static readonly NameInvoker Instance = new NameInvoker();

            public override string Name => nameof(Name);

            public override bool CanWrite => false;

            public override string GetValue(PropertySerializationInfo target) => target.Name;

            public override void SetValue(PropertySerializationInfo target, string value) => target.Name = value;
        }
    }
}
