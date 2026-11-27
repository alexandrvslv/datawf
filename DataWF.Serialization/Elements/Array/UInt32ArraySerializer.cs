using MessagePack;

namespace DataWF.Serialization.Elements;

public class UInt32ArraySerializer : UnmanagedArraySerializer<uint>
{
    public static readonly UInt32ArraySerializer Instance = new();

    public override void Read(ref MessagePackReader reader, scoped ref uint[]? value, MessagePackSerializerOptions options)
    {
        value = MessagePack.Formatters.UInt32ArrayFormatter.Instance.Deserialize(ref reader, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref uint[]? value, MessagePackSerializerOptions options)
    {
        MessagePack.Formatters.UInt32ArrayFormatter.Instance.Serialize(ref writer, value, options);
    }
}
