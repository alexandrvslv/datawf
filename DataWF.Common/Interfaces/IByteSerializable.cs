using System.IO;

namespace DataWF.Common
{
    public interface IByteSerializable
    {
        void Deserialize(byte[] buffer);
        void Deserialize(BinaryReader reader);
        byte[] Serialize();
        void Serialize(BinaryWriter writer);
    }
}