using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public abstract class NullableSerializer<T> : ElementSerializer<T>, IElementSerializer<T?> where T : struct
    {
        protected int size;

        public NullableSerializer() : this(true)
        { }

        public NullableSerializer(bool getSize)
        {
            if (getSize)
            {
                size = Marshal.SizeOf<T>();
            }
        }

        public abstract BinaryToken BinaryToken { get; }

        public override bool CanConvertString => true;

        #region Binary
        public override T Read(SpanReader reader) => reader.Read<T>();

        public override void Write(SpanWriter writer, T value, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write<byte>((byte)BinaryToken);
            }
            writer.Write<T>(value);
        }

        T? IElementSerializer<T?>.Read(SpanReader reader)
        {
            return Read(reader);
        }

        void IElementSerializer<T?>.Write(SpanWriter writer, T? value, bool writeToken)
        {
            if (value != null)
            {
                Write(writer, (T)value, writeToken);
            }
            else
            {
                writer.Write<byte>((byte)BinaryToken.Null);
            }
        }

        T? IElementSerializer<T?>.Read(BinaryReader reader) => Read(reader);

        void IElementSerializer<T?>.Write(BinaryWriter writer, T? value, bool writeToken)
        {
            if (value != null)
            {
                Write(writer, (T)value, writeToken);
            }
            else
            {
                writer.Write((byte)BinaryToken.Null);
            }
        }

        T? IElementSerializer<T?>.Read(BinaryInvokerReader reader, T? value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            return Read(reader, value == null ? default(T) : (T)value, info, map);
        }

        void IElementSerializer<T?>.Write(BinaryInvokerWriter writer, T? value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
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
        T? IElementSerializer<T?>.FromString(string value) => value == null ? (T?)null : FromString(value);

        string IElementSerializer<T?>.ToString(T? value) => value == null ? null : ToString((T)value);

        T? IElementSerializer<T?>.Read(XmlInvokerReader reader, T? value, TypeSerializeInfo info)
        {
            return Read(reader, value == null ? default(T) : (T)value, info);
        }

        void IElementSerializer<T?>.Write(XmlInvokerWriter writer, T? value, TypeSerializeInfo info)
        {
            if (value != null)
            {
                Write(writer, (T)value, info);
            }
        }

        public override object ReadObject(XmlInvokerReader reader, object value, TypeSerializeInfo info)
        {
            return base.Read(reader, default(T), info);
        }
        #endregion
    }
}
