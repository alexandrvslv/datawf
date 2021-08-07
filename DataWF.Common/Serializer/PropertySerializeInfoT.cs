using DataWF.Common;
using System;
using System.Reflection;
using System.Text.Json;

namespace DataWF.Common
{
    public class ObjectPropertySerializeInfo : PropertySerializeInfo<object>
    {
        public override void Write(BinaryInvokerWriter writer, object element)
        {
            if (PropertyInvoker is IValuedInvoker<object> valueInvoker)
            {
                var value = valueInvoker.GetValue(element);
                if (value != null)
                {
                    var serializer = TypeHelper.GetSerializer(value.GetType());
                    serializer.WriteObject(writer, value, null, null);
                }                
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }            
        }

        public override void Write(XmlInvokerWriter writer, object element)
        {
            if (PropertyInvoker is IValuedInvoker<object> valueInvoker)
            {
                var value = valueInvoker.GetValue(element);
                if (value != null)
                {
                    var serializer = TypeHelper.GetSerializer(value.GetType());
                    writer.WriteStart(this);
                    serializer.WriteObject(writer, value, null);
                    writer.WriteEnd(this);
                }
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void Write(Utf8JsonWriter writer, object element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IValuedInvoker<object> valueInvoker)
            {
                var value = valueInvoker.GetValue(element);
                writer.WritePropertyName(JsonName);
                JsonSerializer.Serialize(writer, value, options);
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
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

        public override void Write(BinaryInvokerWriter writer, object element)
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

        public override void Write<E>(BinaryInvokerWriter writer, E element)
        {
            if (PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                TypedSerializer.Write(writer, value, null, null);
            }
            else
            {
                Write(writer, (object)element);
            }
        }

        IElementSerializer<T> GetSetializer(T value)
        {
            return TypedSerializer ?? (IElementSerializer<T>)TypeHelper.GetSerializer(value?.GetType());
        }

        public override void Read(BinaryInvokerReader reader, object element, TypeSerializeInfo itemInfo)
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

        public override void Read<E>(BinaryInvokerReader reader, E element, TypeSerializeInfo itemInfo)
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
                Read(reader, (object)element, itemInfo);
            }
        }

        public override void Write(XmlInvokerWriter writer, object element)
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

        public override void Write<E>(XmlInvokerWriter writer, E element)
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
                Write(writer, (object)element);
            }
        }

        public override void Read(XmlInvokerReader reader, object element, TypeSerializeInfo itemInfo)
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

        public override void Read<E>(XmlInvokerReader reader, E element, TypeSerializeInfo itemInfo)
        {
            if (PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                value = TypedSerializer.Read(reader, value, itemInfo);
                valueInvoker.SetValue(element, value);
            }
            else
            {
                Read(reader, (object)element, itemInfo);
            }
        }

        public override void Write(Utf8JsonWriter writer, object element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                writer.WritePropertyName(JsonName);
                JsonSerializer.Serialize(writer, value, options);
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void Write<E>(Utf8JsonWriter writer, E element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                writer.WritePropertyName(JsonName);
                JsonSerializer.Serialize(writer, value, options);
            }
            else
            {
                Write(writer, (object)element, options);
            }
        }

        public override void Read(ref Utf8JsonReader reader, object element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                var value = JsonSerializer.Deserialize<T>(ref reader, options);
                valueInvoker.SetValue(element, value);
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void Read<E>(ref Utf8JsonReader reader, E element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                var value = JsonSerializer.Deserialize<T>(ref reader, options);
                valueInvoker.SetValue(element, value);
            }
            else
            {
                Read(ref reader, (object)element, options);
            }
        }
    }
}

