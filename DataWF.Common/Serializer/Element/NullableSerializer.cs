using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public abstract class NullableSerializer<T> : ElementSerializer<T>, IElementSerializer<T?> where T : struct
    {
        public override bool CanConvertString => true;

        public override string PropertyToString(object element, IInvoker invoker)
        {
            if (invoker is IValuedInvoker<T?> valueInvoker)
            {
                T? value = valueInvoker.GetValue(element);
                return value == null ? null : ToString((T)value);
            }
            else
            {
                return base.PropertyToString(element, invoker);
            }
        }

        public override string PropertyToString<E>(E element, IInvoker invoker)
        {
            if (invoker is IInvoker<E, T?> valueInvoker)
            {
                T? value = valueInvoker.GetValue(element);
                return value == null ? null : ToString((T)value);
            }
            else
            {
                return base.PropertyToString(element, invoker);
            }
        }

        public override void PropertyFromString(object element, IInvoker invoker, string str)
        {
            if (invoker is IValuedInvoker<T?> valueInvoker)
            {
                if (string.IsNullOrEmpty(str))
                {
                    valueInvoker.SetValue(element, null);
                }
                else
                {
                    T value = FromString(str);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                base.PropertyFromString(element, invoker, str);
            }
        }

        public override void PropertyFromString<E>(E element, IInvoker invoker, string str)
        {
            if (invoker is IInvoker<E, T?> valueInvoker)
            {
                if (string.IsNullOrEmpty(str))
                {
                    valueInvoker.SetValue(element, null);
                }
                else
                {
                    T value = FromString(str);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                base.PropertyFromString(element, invoker, str);
            }
        }

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

        public void ToBinary(BinaryWriter writer, T? value, bool writeToken)
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

        T? IElementSerializer<T?>.FromString(string value) => value == null ? (T?)null : FromString(value);

        public string ToString(T? value) => value == null ? null : ToString((T)value);

        public T? Read(BinaryInvokerReader reader, T? value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map)
        {
            return base.Read(reader, value == null ? default(T) : (T)value, info, map);
        }

        public void Write(BinaryInvokerWriter writer, T? value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map)
        {
            if (value != null)
            {
                base.Write(writer, (T)value, info, map);
            }
            else
            {
                writer.Write((byte)BinaryToken.Null);
            }
        }
    }
}
