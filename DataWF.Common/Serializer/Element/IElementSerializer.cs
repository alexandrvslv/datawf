using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public interface IElementSerializer
    {
        int SizeOfType { get; }
        bool CanConvertString { get; }

        #region Binary
        object ReadObject(BinaryReader reader);
        void WriteObject(BinaryWriter writer, object value, bool writeToken);

        object ReadObject(SpanReader reader);
        void WriteObject(SpanWriter writer, object value, bool writeToken);

        void WriteObject(BinaryInvokerWriter writer, object value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map);
        object ReadObject(BinaryInvokerReader reader, object value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map);
        #endregion

        #region Xml
        object ObjectFromString(string value);
        string ObjectToString(object value);

        void WriteObject(XmlInvokerWriter writer, object value, TypeSerializeInfo info);
        object ReadObject(XmlInvokerReader reader, object value, TypeSerializeInfo info);
        #endregion
    }
}