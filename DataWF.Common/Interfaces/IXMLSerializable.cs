namespace DataWF.Common
{
    public interface IXMLSerializable
    {
        void Serialize(XmlInvokerWriter writer);
        void Deserialize(XmlInvokerReader reader);
    }
}
