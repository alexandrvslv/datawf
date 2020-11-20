using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public abstract class NullableSerializer<T> : ElementSerializer<T>, IElementSerializer<T?> where T : struct
    {
        public override bool CanConvertString => true;
        #region Binary
        public override void PropertyToBinary(BinaryInvokerWriter writer, object element, IInvoker invoker)
        {
            if (invoker is IValuedInvoker<T?> valueInvoker)
            {
                T? value = valueInvoker.GetValue(element);
                if (value == null)
                    writer.WriteNull();
                else
                    Write(writer, (T)value, writer.Serializer.GetTypeInfo<T>(), null);
            }
            else
            {
                base.PropertyToBinary(writer, element, invoker);
            }
        }

        public override void PropertyToBinary<E>(BinaryInvokerWriter writer, E element, IInvoker invoker)
        {
            if (invoker is IInvoker<E, T?> valueInvoker)
            {
                T? value = valueInvoker.GetValue(element);
                if (value == null)
                    writer.WriteNull();
                else
                    Write(writer, (T)value, writer.Serializer.GetTypeInfo<T>(), null);
            }
            else
            {
                base.PropertyToBinary(writer, element, invoker);
            }
        }

        public override void PropertyFromBinary(BinaryInvokerReader reader, object element, IInvoker invoker)
        {
            if (invoker is IValuedInvoker<T?> valueInvoker)
            {
                var token = reader.ReadToken();
                if (token == BinaryToken.Null)
                {
                    valueInvoker.SetValue(element, null);
                }
                else
                {
                    T? value = Read(reader, default(T), reader.Serializer.GetTypeInfo<T>(), null);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                base.PropertyFromBinary(reader, element, invoker);
            }
        }

        public override void PropertyFromBinary<E>(BinaryInvokerReader reader, E element, IInvoker invoker)
        {
            if (invoker is IInvoker<E, T?> valueInvoker)
            {
                var token = reader.ReadToken();
                if (token == BinaryToken.Null)
                {
                    valueInvoker.SetValue(element, null);
                }
                else
                {
                    T? value = Read(reader, default(T), reader.Serializer.GetTypeInfo<T>(), null);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                base.PropertyFromBinary(reader, element, invoker);
            }
        }

        T? IElementSerializer<T?>.FromBinary(BinaryReader reader) => FromBinary(reader);

        void IElementSerializer<T?>.ToBinary(BinaryWriter writer, T? value, bool writeToken)
        {
            if (value != null)
            {
                ToBinary(writer, (T)value, writeToken);
            }
            else
            {
                writer.Write((byte)BinaryToken.Null);
            }
        }

        T? IElementSerializer<T?>.Read(BinaryInvokerReader reader, T? value, TypeSerializationInfo info, Dictionary<ushort, IPropertySerializationInfo> map)
        {
            return Read(reader, value == null ? default(T) : (T)value, info, map);
        }

        void IElementSerializer<T?>.Write(BinaryInvokerWriter writer, T? value, TypeSerializationInfo info, Dictionary<ushort, IPropertySerializationInfo> map)
        {
            if (value != null)
            {
                Write(writer, (T)value, info, map);
            }
            else
            {
                writer.Write((byte)BinaryToken.Null);
            }
        }
        #endregion

        #region Xml
        public override void PropertyToString(XmlInvokerWriter writer, object element, IPropertySerializationInfo property)
        {
            if (property.PropertyInvoker is IValuedInvoker<T?> valueInvoker)
            {
                T? value = valueInvoker.GetValue(element);
                if (value != null)
                {
                    writer.WriteStart(property);
                    Write(writer, (T)value, writer.Serializer.GetTypeInfo<T>());
                    writer.WriteEnd(property);
                }
            }
            else
            {
                base.PropertyToString(writer, element, property);
            }
        }

        public override void PropertyToString<E>(XmlInvokerWriter writer, E element, IPropertySerializationInfo property)
        {
            if (property.PropertyInvoker is IInvoker<E, T?> valueInvoker)
            {
                T? value = valueInvoker.GetValue(element);
                if (value != null)
                {
                    writer.WriteStart(property);
                    Write(writer, (T)value, writer.Serializer.GetTypeInfo<T>());
                    writer.WriteEnd(property);
                }
            }
            else
            {
                base.PropertyToString(writer, element, property);
            }
        }

        public override void PropertyFromString(XmlInvokerReader reader, object element, IPropertySerializationInfo property, TypeSerializationInfo itemInfo)
        {
            if (property.PropertyInvoker is IValuedInvoker<T?> valueInvoker)
            {
                T value = Read(reader, default(T), itemInfo ?? reader.Serializer.GetTypeInfo<T>());
                valueInvoker.SetValue(element, value);
            }
            else
            {
                base.PropertyFromString(reader, element, property, itemInfo);
            }
        }

        public override void PropertyFromString<E>(XmlInvokerReader reader, E element, IPropertySerializationInfo property, TypeSerializationInfo itemInfo)
        {
            if (property.PropertyInvoker is IInvoker<E, T?> valueInvoker)
            {
                T value = Read(reader, default(T), itemInfo ?? reader.Serializer.GetTypeInfo<T>());
                valueInvoker.SetValue(element, value);
            }
            else
            {
                base.PropertyFromString(reader, element, property, itemInfo);
            }
        }

        T? IElementSerializer<T?>.FromString(string value) => value == null ? (T?)null : FromString(value);

        string IElementSerializer<T?>.ToString(T? value) => value == null ? null : ToString((T)value);

        T? IElementSerializer<T?>.Read(XmlInvokerReader reader, T? value, TypeSerializationInfo info)
        {
            return Read(reader, value == null ? default(T) : (T)value, info);
        }

        void IElementSerializer<T?>.Write(XmlInvokerWriter writer, T? value, TypeSerializationInfo info)
        {
            if (value != null)
            {
                Write(writer, (T)value, info);
            }
        }

        public override object Read(XmlInvokerReader reader, object value, TypeSerializationInfo info)
        {
            return base.Read(reader, default(T), info);
        }
        #endregion
    }
}
