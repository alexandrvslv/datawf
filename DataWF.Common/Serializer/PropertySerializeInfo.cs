using Portable.Xaml.Markup;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DataWF.Common
{
    [InvokerGenerator(Instance = true)]
    public abstract partial class PropertySerializeInfo : INamed, IPropertySerializeInfo
    {
        private JsonEncodedText? jsonName;

        public PropertySerializeInfo()
        { }

        public PropertySerializeInfo(PropertyInfo property, int order = -1)
        {
            PropertyInfo = property;
            Name = property.Name;
            DataType = PropertyInfo.PropertyType;
            var keys = PropertySerializeInfoKeys.None;
            if (TypeHelper.IsSerializeText(property))
                keys |= PropertySerializeInfoKeys.Text;
            else if (TypeHelper.IsSerializeAttribute(property))
                keys |= PropertySerializeInfoKeys.Attribute;
            if (TypeHelper.IsSerializeWriteable(property))
                keys |= PropertySerializeInfoKeys.Writeable;
            if (TypeHelper.IsRequired(property))
                keys |= PropertySerializeInfoKeys.Required;
            if (TypeHelper.IsJsonSynchronized(property))
                keys |= PropertySerializeInfoKeys.ChangeSensitive;
            if (TypeHelper.IsReadOnly(property))
                keys |= PropertySerializeInfoKeys.ReadOnly;
            Keys = keys;
            Order = TypeHelper.GetOrder(property, order);
            PropertyInvoker = EmitInvoker.Initialize(property, true);

            Default = TypeHelper.GetDefault(property);
            Serializer = TypeHelper.GetSerializer(property);
        }

        public IInvoker PropertyInvoker { get; }

        public PropertyInfo PropertyInfo { get; }

        public string Name { get; }

        public JsonEncodedText JsonName { get => jsonName ?? (jsonName = JsonEncodedText.Encode(Name, JavaScriptEncoder.UnsafeRelaxedJsonEscaping)).Value; }

        string INamed.Name { get => Name; set => throw new NotSupportedException(); }

        public Type DataType { get; }

        public PropertySerializeInfoKeys Keys { get; }

        public bool IsAttribute => (Keys & PropertySerializeInfoKeys.Attribute) == PropertySerializeInfoKeys.Attribute;

        public bool IsChangeSensitive => (Keys & PropertySerializeInfoKeys.ChangeSensitive) == PropertySerializeInfoKeys.ChangeSensitive;

        public bool IsRequired => (Keys & PropertySerializeInfoKeys.Required) == PropertySerializeInfoKeys.Required;

        public bool IsText => (Keys & PropertySerializeInfoKeys.Text) == PropertySerializeInfoKeys.Text;

        public bool IsWriteable => (Keys & PropertySerializeInfoKeys.Writeable) == PropertySerializeInfoKeys.Writeable;

        public bool IsReadOnly => (Keys & PropertySerializeInfoKeys.ReadOnly) == PropertySerializeInfoKeys.ReadOnly;

        public int Order { get; set; }

        public object Default { get; }

        public IElementSerializer Serializer { get; }

        public V GetValue<V>(object target)
        {
            if (PropertyInvoker is IValuedInvoker<V> valuedInvoker)
            {
                return valuedInvoker.GetValue(target);
            }
            else
            {
                return (V)PropertyInvoker.GetValue(target);
            }
        }

        public void SetValue<V>(object target, V value)
        {
            if (PropertyInvoker is IValuedInvoker<V> valuedInvoker)
            {
                valuedInvoker.SetValue(target, value);
            }
            else
            {
                PropertyInvoker.SetValue(target, value);
            }
        }

        public bool CheckDefault(object value)
        {
            if (Default == null)
                return value == null;
            return Default.Equals(value);
        }

        public abstract void Write(BinaryInvokerWriter writer, object element);

        public abstract void Write<E>(BinaryInvokerWriter writer, E element);

        public abstract void Read(BinaryInvokerReader reader, object element, TypeSerializeInfo itemType);

        public abstract void Read<E>(BinaryInvokerReader reader, E element, TypeSerializeInfo itemType);

        public abstract void Write(XmlInvokerWriter writer, object element);

        public abstract void Write<E>(XmlInvokerWriter writer, E element);

        public abstract void Read(XmlInvokerReader writer, object element, TypeSerializeInfo itemType);

        public abstract void Read<E>(XmlInvokerReader writer, E element, TypeSerializeInfo itemType);

        public abstract void Write(Utf8JsonWriter writer, object element, JsonSerializerOptions options = null);

        public abstract void Write<E>(Utf8JsonWriter writer, E element, JsonSerializerOptions options = null);

        public abstract void Read(ref Utf8JsonReader reader, object element, JsonSerializerOptions options = null);

        public abstract void Read<E>(ref Utf8JsonReader reader, E element, JsonSerializerOptions options = null);

        public override string ToString()
        {
            return $"{Order} {Name} {DataType.Name} {Keys}";
        }


    }
}

