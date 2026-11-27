using System.IO;

namespace DataWF.Serialization;

public interface IBinarySerializable
{
    static abstract T Deserialize<T>(byte[] buffer) where T : IBinarySerializable;
    static abstract T Deserialize<T>(BinaryReader reader) where T : IBinarySerializable;
    byte[] Serialize();
    void Serialize(BinaryWriter writer);
}
