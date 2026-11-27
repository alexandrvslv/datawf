using MessagePack;

namespace DataWF.Serialization.Elements;

public class Int64ArraySerializer : UnmanagedArraySerializer<long>
{
    public static readonly Int64ArraySerializer Instance = new();

    public override void Read(ref MessagePackReader reader, scoped ref long[]? value, MessagePackSerializerOptions options)
    {
        value = MessagePack.Formatters.Int64ArrayFormatter.Instance.Deserialize(ref reader, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref long[]? value, MessagePackSerializerOptions options)
    {
        MessagePack.Formatters.Int64ArrayFormatter.Instance.Serialize(ref writer, value, options);
    }
}
