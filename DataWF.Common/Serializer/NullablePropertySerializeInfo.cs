using DataWF.Common;
using System;
using System.Reflection;
using System.Text.Json;

namespace DataWF.Common
{
    public class NullablePropertySerializeInfo<T> : PropertySerializeInfo<T?> where T : struct
    {
        public NullablePropertySerializeInfo() : base()
        { }

        public NullablePropertySerializeInfo(PropertyInfo property, int order = -1) : base(property, order)
        { }

        public override void Write(BinaryInvokerWriter writer, object element)
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

        public override void Write<E>(BinaryInvokerWriter writer, E element)
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
                Write(writer, (object)element);
            }
        }

        public override void Read(BinaryInvokerReader reader, object element, TypeSerializeInfo itemInfo)
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

        public override void Read<E>(BinaryInvokerReader reader, E element, TypeSerializeInfo itemInfo)
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
                Read(reader, (object)element, itemInfo);
            }
        }

        public override void Write(XmlInvokerWriter writer, object element)
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

        public override void Write<E>(XmlInvokerWriter writer, E element)
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
                Write(writer, (object)element);
            }
        }

        public override void Read(XmlInvokerReader reader, object element, TypeSerializeInfo itemInfo)
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

        public override void Read<E>(XmlInvokerReader reader, E element, TypeSerializeInfo itemInfo)
        {
            if (PropertyInvoker is IInvoker<E, T?> valueInvoker)
            {
                var value = TypedSerializer.Read(reader, null, itemInfo);
                valueInvoker.SetValue(element, value);
            }
            else
            {
                Read(reader, (object)element, itemInfo);
            }
        }

        public override void Write<E>(Utf8JsonWriter writer, E element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IInvoker<E, T?> valueInvoker)
            {
                T? value = valueInvoker.GetValue(element);
                if (value == null)
                    writer.WriteNull(JsonName);
                else
                {
                    writer.WritePropertyName(JsonName);
                    JsonSerializer.Serialize(writer, value, options);
                }
            }
            else
            {
                Write(writer, (object)element, options);
            }
        }

    }
}

