using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{

    public abstract class ElementSerializer : IElementSerializer
    {
        public abstract bool CanConvertString { get; }
        public abstract object ConvertFromString(string value);
        public abstract string ConvertToString(object value);
        public abstract object ConvertFromBinary(BinaryReader reader);
        public abstract void ConvertToBinary(BinaryWriter writer, object value, bool writeToken);

        public virtual void Write(BinaryInvokerWriter writer, object value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map)
        {
            ConvertToBinary(writer.Writer, value, true);
        }

        public virtual object Read(BinaryInvokerReader reader, object value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map)
        {
            return ConvertFromBinary(reader.Reader);
        }

        public abstract string PropertyToString(object element, IInvoker invoker);

        public abstract string PropertyToString<E>(E element, IInvoker invoker);

        public abstract void PropertyFromString(object element, IInvoker invoker, string str);

        public abstract void PropertyFromString<E>(E element, IInvoker invoker, string str);

        public abstract void PropertyToBinary(BinaryInvokerWriter writer, object element, IInvoker invoker);

        public abstract void PropertyToBinary<E>(BinaryInvokerWriter writer, E element, IInvoker invoker);

        public abstract void PropertyFromBinary(BinaryInvokerReader reader, object element, IInvoker invoker);

        public abstract void PropertyFromBinary<E>(BinaryInvokerReader reader, E element, IInvoker invoker);
    }

    public abstract class ElementSerializer<T> : ElementSerializer, IElementSerializer<T>
    {
        public abstract T FromString(string value);

        public abstract string ToString(T value);

        public abstract T FromBinary(BinaryReader reader);

        public abstract void ToBinary(BinaryWriter writer, T value, bool writeToken);

        public override void Write(BinaryInvokerWriter writer, object value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map)
        {
            Write(writer, (T)value, info, map);
        }

        public override object Read(BinaryInvokerReader reader, object value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map)
        {
            return Read(reader, (T)value, info, map);
        }

        public virtual void Write(BinaryInvokerWriter writer, T value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map)
        {
            ToBinary(writer.Writer, value, true);
        }

        public virtual T Read(BinaryInvokerReader reader, T value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map)
        {
            return FromBinary(reader.Reader);
        }

        public override string PropertyToString(object element, IInvoker invoker)
        {
            if (invoker is IValuedInvoker<T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                return ToString(value);
            }
            else
            {
                var value = invoker.GetValue(element);
                if (value == null)
                    return null;
                else
                    return ConvertToString(value);
            }
        }

        public override string PropertyToString<E>(E element, IInvoker invoker)
        {
            if (invoker is IInvoker<E, T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                return ToString(value);
            }
            else
            {
                return PropertyToString((object)element, invoker);
            }
        }

        public override void PropertyFromString(object element, IInvoker invoker, string str)
        {
            if (invoker is IValuedInvoker<T> valueInvoker)
            {
                if (string.IsNullOrEmpty(str))
                {
                    valueInvoker.SetValue(element, default(T));
                }
                else
                {
                    T value = FromString(str);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(str))
                {
                    invoker.SetValue(element, null);
                }
                else
                {
                    var value = ConvertFromString(str);
                    invoker.SetValue(element, value);
                }
            }
        }

        public override void PropertyFromString<E>(E element, IInvoker invoker, string str)
        {
            if (invoker is IInvoker<E, T> valueInvoker)
            {
                if (string.IsNullOrEmpty(str))
                {
                    valueInvoker.SetValue(element, default(T));
                }
                else
                {
                    T value = FromString(str);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                PropertyFromString((object)element, invoker, str);
            }
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
            if (invoker is IValuedInvoker<T> valueInvoker)
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
                var token = reader.ReadToken();
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
    }
}
