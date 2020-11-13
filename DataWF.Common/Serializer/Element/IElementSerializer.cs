using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public interface IElementSerializer
    {
        bool CanConvertString { get; }

        object ConvertFromBinary(BinaryReader reader);
        void ConvertToBinary(BinaryWriter writer, object value, bool writeToken);

        object ConvertFromString(string value);
        string ConvertToString(object value);

        void PropertyToBinary(BinaryInvokerWriter writer, object element, IInvoker invoker);
        void PropertyToBinary<E>(BinaryInvokerWriter writer, E element, IInvoker invoker);

        string PropertyToString(object element, IInvoker invoker);
        string PropertyToString<E>(E element, IInvoker invoker);

        void PropertyFromBinary(BinaryInvokerReader reader, object element, IInvoker invoker);
        void PropertyFromBinary<E>(BinaryInvokerReader reader, E element, IInvoker invoker);

        void PropertyFromString(object element, IInvoker invoker, string str);
        void PropertyFromString<E>(E element, IInvoker invoker, string str);

        void Write(BinaryInvokerWriter writer, object value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map);
        object Read(BinaryInvokerReader reader, object value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map);

    }
}