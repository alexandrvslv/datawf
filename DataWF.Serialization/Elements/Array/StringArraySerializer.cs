using MessagePack;

namespace DataWF.Serialization.Elements;

public class StringArraySerializer : ArraySerializer<string?>
{
    public static readonly StringArraySerializer Instance = new();

    public override void Read(ref MessagePackReader reader, scoped ref string?[]? value, MessagePackSerializerOptions options)
    {
        value = MessagePack.Formatters.NullableStringArrayFormatter.Instance.Deserialize(ref reader, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref string?[]? value, MessagePackSerializerOptions options)
    {
        MessagePack.Formatters.NullableStringArrayFormatter.Instance.Serialize(ref writer, value, options);
    }
}
