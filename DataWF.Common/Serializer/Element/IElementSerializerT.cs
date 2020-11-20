using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public interface IElementSerializer<T> : IElementSerializer
    {
        #region Binary
        T FromBinary(BinaryReader reader);
        void ToBinary(BinaryWriter writer, T value, bool writeToken);

        T Read(BinaryInvokerReader reader, T value, TypeSerializationInfo info, Dictionary<ushort, IPropertySerializationInfo> map);
        void Write(BinaryInvokerWriter writer, T value, TypeSerializationInfo info, Dictionary<ushort, IPropertySerializationInfo> map);
        #endregion

        #region Xml
        T FromString(string value);
        string ToString(T value);

        T Read(XmlInvokerReader reader, T value, TypeSerializationInfo info);
        void Write(XmlInvokerWriter writer, T value, TypeSerializationInfo info);
        #endregion
    }
}