using System.Buffers;
using System.Text.Json;
using DataWF.Serialization;
using MemoryPack;
using MessagePack;

namespace DataWF.Test.Serialization;

public class TestSerializableElement : DataWF.Serialization.ICustomSerializable
{
    private TestClass toSerialize = null!;

    public TestClass ToSerialize
    {
        get => toSerialize;
        set => toSerialize = value;
    }

    public void Deserialize(XmlInvokeReader reader) => reader.Read(ref toSerialize!);

    public void Serialize(XmlInvokeWriter writer) => writer.Write(ref toSerialize!);

    public void Deserialize(ref Utf8JsonReader reader) => throw new System.NotImplementedException();

    public void Serialize(Utf8JsonWriter writer) => throw new System.NotImplementedException();

    public void Deserialize(ref MemoryPackReader reader) => throw new System.NotImplementedException();

    public void Serialize<TBuffer>(ref MemoryPackWriter<TBuffer> writer) where TBuffer : IBufferWriter<byte> => throw new System.NotImplementedException();

    public void Deserialize(ref MessagePackReader reader) => throw new System.NotImplementedException();

    public void Serialize(ref MessagePackWriter writer) => throw new System.NotImplementedException();
}
