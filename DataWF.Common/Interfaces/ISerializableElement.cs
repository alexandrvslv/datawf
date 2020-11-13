namespace DataWF.Common
{
    public interface ISerializableElement
    {
        void Serialize(XmlInvokerWriter writer);
        void Deserialize(XmlInvokerReader reader);
    }
}
