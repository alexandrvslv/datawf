namespace DataWF.Common
{
    public interface ISerializableElement
    {
        void Serialize(ISerializeWriter writer);
        void Deserialize(ISerializeReader reader);
    }
}
