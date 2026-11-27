using System.Buffers;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization;

public interface ICustomSerializable
{
    void Serialize(XmlInvokeWriter writer);
    void Deserialize(XmlInvokeReader reader);

    void Serialize(Utf8JsonWriter writer);
    void Deserialize(ref Utf8JsonReader reader);

    void Serialize<TBuffer>(ref MemoryPackWriter<TBuffer> writer)
        where TBuffer: IBufferWriter<byte>;
    void Deserialize(ref MemoryPackReader reader);

    void Serialize(ref MessagePackWriter writer);
    void Deserialize(ref MessagePackReader reader);
}
