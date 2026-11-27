using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public class CustomSerializer<T> : TypeInfoSerializer<T> where T : ICustomSerializable
{
    public override bool IsAttributeSerializer => false;

    public override void Read(XmlInvokeReader reader, scoped ref T? value)
    {
        value?.Deserialize(reader);
    }

    public override void Write(XmlInvokeWriter writer, scoped ref T? value)
    {
        value?.Serialize(writer);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref T? value, JsonSerializerOptions options)
    {
        value?.Deserialize(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, scoped ref T? value, JsonSerializerOptions options)
    {
        value?.Serialize(writer);
    }

    public override void Read(ref MemoryPackReader reader, scoped ref T? value)
    {
        value?.Deserialize(ref reader);
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T? value)
    {
        value?.Serialize(ref writer);
    }

    public override void Read(ref MessagePackReader reader, scoped ref T? value, MessagePackSerializerOptions options)
    {
        value?.Deserialize(ref reader);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref T? value, MessagePackSerializerOptions options)
    {
        value?.Serialize(ref writer);
    }

}
