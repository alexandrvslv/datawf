using MessagePack;

namespace DataWF.Serialization.Elements;

public class SByteArraySerializer : UnmanagedArraySerializer<sbyte>
{
    public static SByteArraySerializer Instance = new();

    public override void Read(ref MessagePackReader reader, scoped ref sbyte[]? value, MessagePackSerializerOptions options)
    {
        value = MessagePack.Formatters.SByteArrayFormatter.Instance.Deserialize(ref reader, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref sbyte[]? value, MessagePackSerializerOptions options)
    {
        MessagePack.Formatters.SByteArrayFormatter.Instance.Serialize(ref writer, value, options);
    }
}
