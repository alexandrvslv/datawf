using DataWF.Common;
using Portable.Xaml.Markup;
using System;
using System.Diagnostics;
using System.Reflection;

[assembly: Invoker(typeof(PropertySerializeInfo), nameof(PropertySerializeInfo.IsAttribute), typeof(PropertySerializeInfo.IsAttributeInvoker))]
[assembly: Invoker(typeof(PropertySerializeInfo), nameof(PropertySerializeInfo.Order), typeof(PropertySerializeInfo.OrderInvoker))]
[assembly: Invoker(typeof(PropertySerializeInfo), nameof(PropertySerializeInfo.Name), typeof(PropertySerializeInfo.NameInvoker))]
namespace DataWF.Common
{
    public abstract class PropertySerializeInfo : INamed, IPropertySerializeInfo
    {
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

        public string Name { get; set; }

        public Type DataType { get; set; }

        public PropertySerializeInfoKeys Keys { get; }

        public bool IsAttribute => (Keys & PropertySerializeInfoKeys.Attribute) == PropertySerializeInfoKeys.Attribute;

        public bool IsChangeSensitive => (Keys & PropertySerializeInfoKeys.ChangeSensitive) == PropertySerializeInfoKeys.ChangeSensitive;

        public bool IsRequired => (Keys & PropertySerializeInfoKeys.Required) == PropertySerializeInfoKeys.Required;

        public bool IsText => (Keys & PropertySerializeInfoKeys.Text) == PropertySerializeInfoKeys.Text;

        public bool IsWriteable => (Keys & PropertySerializeInfoKeys.Writeable) == PropertySerializeInfoKeys.Writeable;

        public bool IsReadOnly => (Keys & PropertySerializeInfoKeys.ReadOnly) == PropertySerializeInfoKeys.ReadOnly;

        public int Order { get; set; }

        public object Default { get; }

        public ElementSerializer Serializer { get; }

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

        public abstract void PropertyToBinary(BinaryInvokerWriter writer, object element);

        public abstract void PropertyToBinary<E>(BinaryInvokerWriter writer, E element);

        public abstract void PropertyFromBinary(BinaryInvokerReader reader, object element, TypeSerializeInfo itemType);

        public abstract void PropertyFromBinary<E>(BinaryInvokerReader reader, E element, TypeSerializeInfo itemType);

        public abstract void PropertyToString(XmlInvokerWriter writer, object element);

        public abstract void PropertyToString<E>(XmlInvokerWriter writer, E element);

        public abstract void PropertyFromString(XmlInvokerReader writer, object element, TypeSerializeInfo itemType);

        public abstract void PropertyFromString<E>(XmlInvokerReader writer, E element, TypeSerializeInfo itemType);

        public override string ToString()
        {
            return $"{Order} {Name} {DataType.Name} {Keys}";
        }

        public class IsAttributeInvoker : Invoker<IPropertySerializeInfo, bool>
        {
            public static readonly IsAttributeInvoker Instance = new IsAttributeInvoker();

            public override string Name => nameof(IsAttribute);

            public override bool CanWrite => false;

            public override bool GetValue(IPropertySerializeInfo target) => target.IsAttribute;

            public override void SetValue(IPropertySerializeInfo target, bool value) { }
        }

        public class OrderInvoker : Invoker<IPropertySerializeInfo, int>
        {
            public static readonly OrderInvoker Instance = new OrderInvoker();

            public override string Name => nameof(Order);

            public override bool CanWrite => false;

            public override int GetValue(IPropertySerializeInfo target) => target.Order;

            public override void SetValue(IPropertySerializeInfo target, int value) => target.Order = value;
        }

        public class NameInvoker : Invoker<IPropertySerializeInfo, string>
        {
            public static readonly NameInvoker Instance = new NameInvoker();

            public override string Name => nameof(Name);

            public override bool CanWrite => false;

            public override string GetValue(IPropertySerializeInfo target) => target.Name;

            public override void SetValue(IPropertySerializeInfo target, string value) => target.Name = value;
        }
    }

    public class PropertySerializeInfo<T> : PropertySerializeInfo
    {
        private IElementSerializer<T> serializer;

        public PropertySerializeInfo() : base()
        { }

        public PropertySerializeInfo(PropertyInfo property, int order = -1) : base(property, order)
        { }

        public IElementSerializer<T> TypedSerializer => serializer ?? (serializer = Serializer as IElementSerializer<T>);

        public T GetValue<E>(E target)
        {
            if (PropertyInvoker is IInvoker<E, T> valuedInvoker)
            {
                return valuedInvoker.GetValue(target);
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public void SetValue<E>(E target, T value)
        {
            if (PropertyInvoker is IInvoker<E, T> valuedInvoker)
            {
                valuedInvoker.SetValue(target, value);
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void PropertyToBinary(BinaryInvokerWriter writer, object element)
        {
            if (PropertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                TypedSerializer.Write(writer, value, null, null);
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void PropertyToBinary<E>(BinaryInvokerWriter writer, E element)
        {
            if (PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                TypedSerializer.Write(writer, value, null, null);
            }
            else
            {
                PropertyToBinary(writer, (object)element);
            }
        }

        public override void PropertyFromBinary(BinaryInvokerReader reader, object element, TypeSerializeInfo itemInfo)
        {
            var token = reader.ReadToken();
            if (PropertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                if (token == BinaryToken.Null)
                {
                    valueInvoker.SetValue(element, default(T));
                }
                else
                {
                    T value = TypedSerializer.Read(reader, default(T), null, null);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void PropertyFromBinary<E>(BinaryInvokerReader reader, E element, TypeSerializeInfo itemInfo)
        {
            if (PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                var token = reader.ReadToken();
                if (token == BinaryToken.Null)
                {
                    valueInvoker.SetValue(element, default(T));
                }
                else
                {
                    T value = TypedSerializer.Read(reader, default(T), null, null);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                PropertyFromBinary(reader, (object)element, itemInfo);
            }
        }

        public override void PropertyToString(XmlInvokerWriter writer, object element)
        {
            if (PropertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                writer.WriteStart(this);
                TypedSerializer.Write(writer, value, null);
                writer.WriteEnd(this);
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void PropertyToString<E>(XmlInvokerWriter writer, E element)
        {
            if (PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                writer.WriteStart(this);
                TypedSerializer.Write(writer, value, null);
                writer.WriteEnd(this);
            }
            else
            {
                PropertyToString(writer, (object)element);
            }
        }

        public override void PropertyFromString(XmlInvokerReader reader, object element, TypeSerializeInfo itemInfo)
        {
            if (PropertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                value = TypedSerializer.Read(reader, value, itemInfo);
                valueInvoker.SetValue(element, value);
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void PropertyFromString<E>(XmlInvokerReader reader, E element, TypeSerializeInfo itemInfo)
        {
            if (PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                value = TypedSerializer.Read(reader, value, itemInfo);
                valueInvoker.SetValue(element, value);
            }
            else
            {
                PropertyFromString(reader, (object)element, itemInfo);
            }
        }
    }
    public class ReferencePropertySerializeInfo<T> : PropertySerializeInfo<T> where T : class
    {
        public ReferencePropertySerializeInfo() : base()
        { }

        public ReferencePropertySerializeInfo(PropertyInfo property, int order = -1) : base(property, order)
        { }

        public override void PropertyToString(XmlInvokerWriter writer, object element)
        {
            if (PropertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                if (value != null)
                {
                    writer.WriteStart(this);
                    TypedSerializer.Write(writer, value, writer.Serializer.GetTypeInfo(value.GetType()));
                    writer.WriteEnd(this);
                }
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void PropertyToString<E>(XmlInvokerWriter writer, E element)
        {
            if (PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                if (value != null)
                {
                    writer.WriteStart(this);
                    TypedSerializer.Write(writer, value, writer.Serializer.GetTypeInfo<T>());
                    writer.WriteEnd(this);
                }
            }
            else
            {
                PropertyToString(writer, (object)element);
            }
        }
    }

    public class NullablePropertySerializeInfo<T> : PropertySerializeInfo<T?> where T : struct
    {
        public NullablePropertySerializeInfo() : base()
        { }

        public NullablePropertySerializeInfo(PropertyInfo property, int order = -1) : base(property, order)
        { }

        public override void PropertyToBinary(BinaryInvokerWriter writer, object element)
        {
            if (PropertyInvoker is IValuedInvoker<T?> valueInvoker)
            {
                T? value = valueInvoker.GetValue(element);
                if (value == null)
                    writer.WriteNull();
                else
                    TypedSerializer.Write(writer, (T)value, null, null);
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void PropertyToBinary<E>(BinaryInvokerWriter writer, E element)
        {
            if (PropertyInvoker is IInvoker<E, T?> valueInvoker)
            {
                T? value = valueInvoker.GetValue(element);
                if (value == null)
                    writer.WriteNull();
                else
                    TypedSerializer.Write(writer, (T)value, null, null);
            }
            else
            {
                PropertyToBinary(writer, (object)element);
            }
        }

        public override void PropertyFromBinary(BinaryInvokerReader reader, object element, TypeSerializeInfo itemInfo)
        {
            if (PropertyInvoker is IValuedInvoker<T?> valueInvoker)
            {
                var token = reader.ReadToken();
                if (token == BinaryToken.Null)
                {
                    valueInvoker.SetValue(element, null);
                }
                else
                {
                    T? value = TypedSerializer.Read(reader, default(T), null, null);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void PropertyFromBinary<E>(BinaryInvokerReader reader, E element, TypeSerializeInfo itemInfo)
        {
            if (PropertyInvoker is IInvoker<E, T?> valueInvoker)
            {
                var token = reader.ReadToken();
                if (token == BinaryToken.Null)
                {
                    valueInvoker.SetValue(element, null);
                }
                else
                {
                    T? value = TypedSerializer.Read(reader, default(T), null, null);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                PropertyFromBinary(reader, (object)element, itemInfo);
            }
        }

        public override void PropertyToString(XmlInvokerWriter writer, object element)
        {
            if (PropertyInvoker is IValuedInvoker<T?> valueInvoker)
            {
                T? value = valueInvoker.GetValue(element);
                if (value != null)
                {
                    writer.WriteStart(this);
                    TypedSerializer.Write(writer, (T)value, null);
                    writer.WriteEnd(this);
                }
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void PropertyToString<E>(XmlInvokerWriter writer, E element)
        {
            if (PropertyInvoker is IInvoker<E, T?> valueInvoker)
            {
                T? value = valueInvoker.GetValue(element);
                if (value != null)
                {
                    writer.WriteStart(this);
                    TypedSerializer.Write(writer, (T)value, null);
                    writer.WriteEnd(this);
                }
            }
            else
            {
                PropertyToString(writer, (object)element);
            }
        }

        public override void PropertyFromString(XmlInvokerReader reader, object element, TypeSerializeInfo itemInfo)
        {
            if (PropertyInvoker is IValuedInvoker<T?> valueInvoker)
            {
                var value = TypedSerializer.Read(reader, null, itemInfo);
                valueInvoker.SetValue(element, value);
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void PropertyFromString<E>(XmlInvokerReader reader, E element, TypeSerializeInfo itemInfo)
        {
            if (PropertyInvoker is IInvoker<E, T?> valueInvoker)
            {
                var value = TypedSerializer.Read(reader, null, itemInfo);
                valueInvoker.SetValue(element, value);
            }
            else
            {
                PropertyFromString(reader, (object)element, itemInfo);
            }
        }


    }
}

