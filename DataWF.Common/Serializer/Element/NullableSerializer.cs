using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public abstract class NullableSerializer<T> : ElementSerializer<T>, IElementSerializer<T?> where T : struct
    {
        public override bool CanConvertString => true;

        public override string FromProperty(object element, IInvoker invoker)
        {
            if (invoker is IValuedInvoker<T?> valueInvoker)
            {
                T? value = valueInvoker.GetValue(element);
                return value == null ? null : ToString((T)value);
            }
            else
            {
                return base.FromProperty(element, invoker);
            }
        }

        public override string FromProperty<E>(E element, IInvoker invoker)
        {
            if (invoker is IInvoker<E, T?> valueInvoker)
            {
                T? value = valueInvoker.GetValue(element);
                return value == null ? null : ToString((T)value);
            }
            else
            {
                return base.FromProperty(element, invoker);
            }
        }

        public override void ToProperty(object element, IInvoker invoker, string str)
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
                base.ToProperty(element, invoker, str);
            }
        }

        public override void ToProperty<E>(E element, IInvoker invoker, string str)
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
                base.ToProperty(element, invoker, str);
            }
        }

        public override void FromProperty(BinaryWriter writer, object element, IInvoker invoker)
        {
            if (invoker is IValuedInvoker<T?> valueInvoker)
            {
                T? value = valueInvoker.GetValue(element);
                if (value == null)
                    writer.Write((byte)BinaryToken.Null);
                else
                    ToBinary(writer, (T)value, true);
            }
            else
            {
                base.FromProperty(writer, element, invoker);
            }
        }

        public override void FromProperty<E>(BinaryWriter writer, E element, IInvoker invoker)
        {
            if (invoker is IInvoker<E, T?> valueInvoker)
            {
                T? value = valueInvoker.GetValue(element);
                if (value == null)
                    writer.Write((byte)BinaryToken.Null);
                else
                    ToBinary(writer, (T)value, true);
            }
            else
            {
                base.FromProperty(writer, element, invoker);
            }
        }

        public override void ToProperty(BinaryReader reader, object element, IInvoker invoker)
        {
            if (invoker is IValuedInvoker<T?> valueInvoker)
            {
                var token = (BinaryToken)reader.ReadByte();
                if (token == BinaryToken.Null)
                {
                    valueInvoker.SetValue(element, null);
                }
                else
                {
                    T? value = FromBinary(reader);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                base.ToProperty(reader, element, invoker);
            }
        }

        public override void ToProperty<E>(BinaryReader reader, E element, IInvoker invoker)
        {
            if (invoker is IInvoker<E, T?> valueInvoker)
            {
                var token = (BinaryToken)reader.ReadByte();
                if (token == BinaryToken.Null)
                {
                    valueInvoker.SetValue(element, null);
                }
                else
                {
                    T? value = FromBinary(reader);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                base.ToProperty(reader, element, invoker);
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
            return Read(reader, value == null ? default(T) : (T)value, info, map);
        }

        public void Write(BinaryInvokerWriter writer, T? value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map)
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
    }
}
