using System.IO;

namespace DataWF.Common
{
    public interface IBinarySerializable
    {
        void Deserialize(byte[] buffer);
        void Deserialize(BinaryReader reader);
        byte[] Serialize();
        void Serialize(BinaryWriter writer);
    }
}