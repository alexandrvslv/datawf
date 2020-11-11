using System;
using System.IO;

namespace DataWF.Common
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class ElementSerializerAttribute : Attribute
    {
        public ElementSerializerAttribute(Type serializerType)
        {
            SerializerType = serializerType;
        }

        public Type SerializerType { get; set; }
    }

    public abstract class ElementSerializer
    {
        public abstract object ConvertFromString(string value);
        public abstract string ConvertToString(object value);
        public abstract object ConvertFromBinary(BinaryReader reader);
        public abstract void ConvertToBinary(object value, BinaryWriter writer, bool writeToken);

        public abstract string FromProperty(object element, IInvoker invoker);

        public abstract string FromProperty<E>(E element, IInvoker invoker);

        public abstract void ToProperty(string str, object element, IInvoker invoker);

        public abstract void ToProperty<E>(string str, E element, IInvoker invoker);

        public abstract void FromProperty(BinaryWriter writer, object element, IInvoker invoker);

        public abstract void FromProperty<E>(BinaryWriter writer, E element, IInvoker invoker);

        public abstract void ToProperty(BinaryReader reader, object element, IInvoker invoker);

        public abstract void ToProperty<E>(BinaryReader reader, E element, IInvoker invoker);
    }

    public abstract class ElementSerializer<T> : ElementSerializer
    {
        public abstract T FromString(string value);
        public abstract string ToString(T value);

        public abstract T FromBinary(BinaryReader reader);

        public abstract void ToBinary(T value, BinaryWriter writer, bool writeToken);

        public override string FromProperty(object element, IInvoker invoker)
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

        public override string FromProperty<E>(E element, IInvoker invoker)
        {
            if (invoker is IInvoker<E, T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                return ToString(value);
            }
            else
            {
                return FromProperty((object)element, invoker);
            }
        }

        public override void ToProperty(string str, object element, IInvoker invoker)
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

        public override void ToProperty<E>(string str, E element, IInvoker invoker)
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
                ToProperty(str, (object)element, invoker);
            }
        }

        public override void FromProperty(BinaryWriter writer, object element, IInvoker invoker)
        {
            if (invoker is IValuedInvoker<T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                ToBinary(value, writer, true);
            }
            else
            {
                var value = invoker.GetValue(element);
                if (value == null)
                    writer.Write((byte)BinaryToken.Null);
                else
                    ConvertToBinary(value, writer, true);
            }
        }

        public override void FromProperty<E>(BinaryWriter writer, E element, IInvoker invoker)
        {
            if (invoker is IInvoker<E, T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                ToBinary(value, writer, true);
            }
            else
            {
                FromProperty(writer, (object)element, invoker);
            }
        }

        public override void ToProperty(BinaryReader reader, object element, IInvoker invoker)
        {
            if (invoker is IValuedInvoker<T> valueInvoker)
            {
                var token = (BinaryToken)reader.ReadByte();
                if (token == BinaryToken.Null)
                {
                    valueInvoker.SetValue(element, default(T));
                }
                else
                {
                    T value = FromBinary(reader);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                var token = (BinaryToken)reader.ReadByte();
                if (token == BinaryToken.Null)
                {
                    invoker.SetValue(element, null);
                }
                else
                {
                    var value = ConvertFromBinary(reader);
                    invoker.SetValue(element, value);
                }
            }
        }

        public override void ToProperty<E>(BinaryReader reader, E element, IInvoker invoker)
        {
            if (invoker is IInvoker<E, T> valueInvoker)
            {
                var token = (BinaryToken)reader.ReadByte();
                if (token == BinaryToken.Null)
                {
                    valueInvoker.SetValue(element, default(T));
                }
                else
                {
                    T value = FromBinary(reader);
                    valueInvoker.SetValue(element, value);
                }
            }
            else
            {
                ToProperty(reader, (object)element, invoker);
            }
        }
    }

    public abstract class StructSerializer<T> : ElementSerializer<T> where T : struct
    {
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

        public override void ToProperty(string str, object element, IInvoker invoker)
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
                base.ToProperty(str, element, invoker);
            }
        }

        public override void ToProperty<E>(string str, E element, IInvoker invoker)
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
                base.ToProperty(str, element, invoker);
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
                    ToBinary((T)value, writer, true);
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
                    ToBinary((T)value, writer, true);
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
    }
}
