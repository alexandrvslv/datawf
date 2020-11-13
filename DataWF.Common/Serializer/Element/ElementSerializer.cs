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

        public abstract string FromProperty(object element, IInvoker invoker);

        public abstract string FromProperty<E>(E element, IInvoker invoker);

        public abstract void ToProperty(object element, IInvoker invoker, string str);

        public abstract void ToProperty<E>(E element, IInvoker invoker, string str);

        public abstract void FromProperty(BinaryWriter writer, object element, IInvoker invoker);

        public abstract void FromProperty<E>(BinaryWriter writer, E element, IInvoker invoker);

        public abstract void ToProperty(BinaryReader reader, object element, IInvoker invoker);

        public abstract void ToProperty<E>(BinaryReader reader, E element, IInvoker invoker);
    }

    public abstract class ElementSerializer<T> : ElementSerializer, IElementSerializer<T>
    {
        public abstract T FromString(string value);
        public abstract string ToString(T value);

        public abstract T FromBinary(BinaryReader reader);

        public abstract void ToBinary(BinaryWriter writer, T value, bool writeToken);

        public virtual void Write(BinaryInvokerWriter writer, T value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map)
        {
            ToBinary(writer.Writer, value, true);
        }

        public virtual T Read(BinaryInvokerReader reader, T value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map)
        {
            return FromBinary(reader.Reader);
        }

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

        public override void ToProperty(object element, IInvoker invoker, string str)
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

        public override void ToProperty<E>(E element, IInvoker invoker, string str)
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
                ToProperty((object)element, invoker, str);
            }
        }

        public override void FromProperty(BinaryWriter writer, object element, IInvoker invoker)
        {
            if (invoker is IValuedInvoker<T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                ToBinary(writer, value, true);
            }
            else
            {
                var value = invoker.GetValue(element);
                if (value == null)
                    writer.Write((byte)BinaryToken.Null);
                else
                    ConvertToBinary(writer, value, true);
            }
        }

        public override void FromProperty<E>(BinaryWriter writer, E element, IInvoker invoker)
        {
            if (invoker is IInvoker<E, T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                ToBinary(writer, value, true);
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
}
