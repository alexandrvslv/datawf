using DataWF.Common;
using System;
using System.Reflection;

namespace DataWF.Common
{
    public class ReferencePropertySerializeInfo<T> : PropertySerializeInfo<T> where T : class
    {
        public ReferencePropertySerializeInfo() : base()
        { }

        public ReferencePropertySerializeInfo(PropertyInfo property, int order = -1) : base(property, order)
        { }

        public override void Write(XmlInvokerWriter writer, object element)
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

        public override void Write<E>(XmlInvokerWriter writer, E element)
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
                Write(writer, (object)element);
            }
        }
    }
}

