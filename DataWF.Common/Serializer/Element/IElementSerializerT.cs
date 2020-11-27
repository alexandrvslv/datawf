using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public interface IElementSerializer<T> : IElementSerializer
    {
        #region Binary
        T Read(BinaryReader reader);
        void Write(BinaryWriter writer, T value, bool writeToken);

        T Read(SpanReader reader);
        void Write(SpanWriter writer, T value, bool writeToken);

        T Read(BinaryInvokerReader reader, T value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map);
        void Write(BinaryInvokerWriter writer, T value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map);

        #endregion

        #region Xml
        T FromString(string value);
        string ToString(T value);

        T Read(XmlInvokerReader reader, T value, TypeSerializeInfo info);
        void Write(XmlInvokerWriter writer, T value, TypeSerializeInfo info);
        #endregion
    }
}