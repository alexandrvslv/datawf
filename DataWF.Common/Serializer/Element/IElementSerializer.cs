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

        void FromProperty(BinaryWriter writer, object element, IInvoker invoker);
        void FromProperty<E>(BinaryWriter writer, E element, IInvoker invoker);

        string FromProperty(object element, IInvoker invoker);
        string FromProperty<E>(E element, IInvoker invoker);

        void ToProperty(BinaryReader reader, object element, IInvoker invoker);
        void ToProperty<E>(BinaryReader reader, E element, IInvoker invoker);

        void ToProperty(object element, IInvoker invoker, string str);
        void ToProperty<E>(E element, IInvoker invoker, string str);

        void Write(BinaryInvokerWriter writer, object value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map);
        object Read(BinaryInvokerReader reader, object value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map);

    }
}