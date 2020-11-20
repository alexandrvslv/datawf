using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DataWF.Common
{
    public abstract class ElementSerializer<T> : ElementSerializer, IElementSerializer<T>
    {
        #region Binary
        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(BinaryWriter writer, object value, bool writeToken) => ToBinary(writer, (T)value, writeToken);

        public abstract T FromBinary(BinaryReader reader);

        public abstract void ToBinary(BinaryWriter writer, T value, bool writeToken);

        public override void Write(BinaryInvokerWriter writer, object value, TypeSerializationInfo info, Dictionary<ushort, IPropertySerializationInfo> map)
        {
            Write(writer, (T)value, info, map);
        }

        public override object Read(BinaryInvokerReader reader, object value, TypeSerializationInfo info, Dictionary<ushort, IPropertySerializationInfo> map)
        {
            return Read(reader, (T)value, info, map);
        }

        public virtual void Write(BinaryInvokerWriter writer, T value, TypeSerializationInfo info, Dictionary<ushort, IPropertySerializationInfo> map)
        {
            ToBinary(writer.Writer, value, true);
        }

        public virtual T Read(BinaryInvokerReader reader, T value, TypeSerializationInfo info, Dictionary<ushort, IPropertySerializationInfo> map)
        {
            return FromBinary(reader.Reader);
        }

        public override void PropertyToBinary(BinaryInvokerWriter writer, object element, IInvoker invoker)
        {
            if (invoker is IValuedInvoker<T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                Write(writer, value, writer.Serializer.GetTypeInfo<T>(), null);
            }
            else
            {
                var value = invoker.GetValue(element);
                if (value == null)
                    writer.WriteNull();
                else
                    Write(writer, value, writer.Serializer.GetTypeInfo<T>(), null);
            }
        }

        public override void PropertyToBinary<E>(BinaryInvokerWriter writer, E element, IInvoker invoker)
        {
            if (invoker is IInvoker<E, T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                Write(writer, value, writer.Serializer.GetTypeInfo<T>(), null);
            }
            else
            {
                PropertyToBinary(writer, (object)element, invoker);
            }
        }

        public override void PropertyFromBinary(BinaryInvokerReader reader, object element, IInvoker invoker)
        {
            var token = reader.ReadToken();
            if (invoker is IValuedInvoker<T> valueInvoker)
            {
                if (token == BinaryToken.Null)
                {
                    valueInvoker.SetValue(element, default(T));
                }
                else
                {
                    T value = Read(reader, default(T), reader.Serializer.GetTypeInfo<T>(), null);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                if (token == BinaryToken.Null)
                {
                    invoker.SetValue(element, null);
                }
                else
                {
                    var value = Read(reader, default(T), reader.Serializer.GetTypeInfo<T>(), null);
                    invoker.SetValue(element, value);
                }
            }
        }

        public override void PropertyFromBinary<E>(BinaryInvokerReader reader, E element, IInvoker invoker)
        {
            if (invoker is IInvoker<E, T> valueInvoker)
            {
                var token = reader.ReadToken();
                if (token == BinaryToken.Null)
                {
                    valueInvoker.SetValue(element, default(T));
                }
                else
                {
                    T value = Read(reader, default(T), reader.Serializer.GetTypeInfo<T>(), null);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                PropertyFromBinary(reader, (object)element, invoker);
            }
        }
        #endregion

        #region Xml
        public override object ConvertFromString(string value) => FromString(value);

        public override string ConvertToString(object value) => ToString((T)value);

        public abstract T FromString(string value);

        public abstract string ToString(T value);

        public override void Write(XmlInvokerWriter writer, object value, TypeSerializationInfo info)
        {
            Write(writer, (T)value, info);
        }

        public override object Read(XmlInvokerReader reader, object value, TypeSerializationInfo info)
        {
            return Read(reader, (T)value, info);
        }

        public virtual void Write(XmlInvokerWriter writer, T value, TypeSerializationInfo info)
        {
            writer.Writer.WriteString(ToString(value));
        }

        public virtual T Read(XmlInvokerReader reader, T value, TypeSerializationInfo info)
        {
            var str = reader.Reader.NodeType == System.Xml.XmlNodeType.Attribute
                ? reader.Reader.ReadContentAsString()
                : reader.Reader.ReadElementContentAsString();
            return FromString(str);
        }

        public override void PropertyToString(XmlInvokerWriter writer, object element, IPropertySerializationInfo property)
        {
            if (property.PropertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                writer.WriteStart(property);
                Write(writer, value, writer.Serializer.GetTypeInfo<T>());
                writer.WriteEnd(property);
            }
            else
            {
                var value = property.PropertyInvoker.GetValue(element);
                if (value != null)
                {
                    writer.WriteStart(property);
                    Write(writer, value, writer.Serializer.GetTypeInfo<T>());
                    writer.WriteEnd(property);
                }
            }
        }

        public override void PropertyToString<E>(XmlInvokerWriter writer, E element, IPropertySerializationInfo property)
        {
            if (property.PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                writer.WriteStart(property);
                Write(writer, value, writer.Serializer.GetTypeInfo<T>());
                writer.WriteEnd(property);
            }
            else
            {
                PropertyToString(writer, (object)element, property);
            }
        }

        public override void PropertyFromString(XmlInvokerReader reader, object element, IPropertySerializationInfo property, TypeSerializationInfo itemInfo)
        {
            if (property.PropertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                T value = Read(reader, default(T), itemInfo ?? reader.Serializer.GetTypeInfo<T>());
                valueInvoker.SetValue(element, value);
            }
            else
            {
                T value = Read(reader, default(T), itemInfo ?? reader.Serializer.GetTypeInfo<T>());
                property.PropertyInvoker.SetValue(element, value);
            }
        }

        public override void PropertyFromString<E>(XmlInvokerReader reader, E element, IPropertySerializationInfo property, TypeSerializationInfo itemInfo)
        {
            if (property.PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                T value = Read(reader, default(T), itemInfo ?? reader.Serializer.GetTypeInfo<T>());
                valueInvoker.SetValue(element, value);
            }
            else
            {
                PropertyFromString(reader, (object)element, property, itemInfo);
            }
        }
        #endregion

        #region Json
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            Write(writer, (T)value, options);
        }

        public override object Read(ref Utf8JsonReader reader, object value, JsonSerializerOptions options)
        {
            return Read(ref reader, value, options);
        }

        public virtual void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }

        public virtual T Read(ref Utf8JsonReader reader, T value, JsonSerializerOptions options)
        {
            return FromString(reader.GetString());
        }
        #endregion
    }
}
