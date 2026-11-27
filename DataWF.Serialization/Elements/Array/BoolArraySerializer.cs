using MessagePack;

namespace DataWF.Serialization.Elements;

public class BoolArraySerializer : UnmanagedArraySerializer<bool>
{
    public static readonly BoolArraySerializer Instance = new();

    public override void Read(ref MessagePackReader reader, scoped ref bool[]? value, MessagePackSerializerOptions options)
    {
        value = MessagePack.Formatters.BooleanArrayFormatter.Instance.Deserialize(ref reader, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref bool[]? value, MessagePackSerializerOptions options)
    {
        MessagePack.Formatters.BooleanArrayFormatter.Instance.Serialize(ref writer, value, options);
    }
}