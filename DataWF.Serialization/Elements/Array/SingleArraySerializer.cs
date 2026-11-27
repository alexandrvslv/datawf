using MessagePack;

namespace DataWF.Serialization.Elements;

public class SingleArraySerializer : UnmanagedArraySerializer<float>
{
    public static readonly SingleArraySerializer Instance = new();
    public override void Read(ref MessagePackReader reader, scoped ref float[]? value, MessagePackSerializerOptions options)
    {
        value = MessagePack.Formatters.SingleArrayFormatter.Instance.Deserialize(ref reader, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref float[]? value, MessagePackSerializerOptions options)
    {
        MessagePack.Formatters.SingleArrayFormatter.Instance.Serialize(ref writer, value, options);
    }
}
