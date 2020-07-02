namespace DataWF.Common
{
    public interface IByteSerializable
    {
        void Deserialize(byte[] buffer);
        byte[] Serialize();
    }
}