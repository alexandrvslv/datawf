using System;
using System.IO;

namespace DataWF.Common
{
    public class XMLSerializer<T> : ObjectSerializer<T> where T : IXMLSerializable
    {
        public override bool CanConvertString => false;

        public override void Write(XmlInvokerWriter writer, T value, TypeSerializationInfo info)
        {
            value.Serialize(writer);
        }

        public override T Read(XmlInvokerReader reader, T value, TypeSerializationInfo typeInfo)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Comment)
                typeInfo = reader.ReadType(typeInfo);
            if (value == null || value.GetType() != typeInfo.Type)
            {
                var length = int.TryParse(reader.Reader.GetAttribute("Count"), out int count) ? count : 1;
                value = (T)(typeInfo.ListConstructor?.Create(length) ?? typeInfo.Constructor?.Create());
            }
            value.Deserialize(reader);

            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.Read();
            }
            return value;
        }

    }
}
