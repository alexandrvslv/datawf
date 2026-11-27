using MessagePack;

namespace DataWF.Serialization.Elements;

public class Int16ArraySerializer : UnmanagedArraySerializer<short>
{
    public static readonly Int16ArraySerializer Instance = new();

    public override void Read(ref MessagePackReader reader, scoped ref short[]? value, MessagePackSerializerOptions options)
    {
        value = MessagePack.Formatters.Int16ArrayFormatter.Instance.Deserialize(ref reader, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref short[]? value, MessagePackSerializerOptions options)
    {
        MessagePack.Formatters.Int16ArrayFormatter.Instance.Serialize(ref writer, value, options);
    }
}
