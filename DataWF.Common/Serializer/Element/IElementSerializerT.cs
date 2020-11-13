using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public interface IElementSerializer<T> : IElementSerializer
    {
        T FromBinary(BinaryReader reader);
        void ToBinary(BinaryWriter writer, T value, bool writeToken);

        T FromString(string value);
        string ToString(T value);

        T Read(BinaryInvokerReader reader, T value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map);
        void Write(BinaryInvokerWriter writer, T value, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map);
    }
}