using MessagePack;

namespace DataWF.Serialization.Elements;

public class Int32ArraySerializer : UnmanagedArraySerializer<int>
{
    public static readonly Int32ArraySerializer Instance = new Int32ArraySerializer();

    public override void Read(ref MessagePackReader reader, scoped ref int[]? value, MessagePackSerializerOptions options)
    {
        value = MessagePack.Formatters.Int32ArrayFormatter.Instance.Deserialize(ref reader, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref int[]? value, MessagePackSerializerOptions options)
    {
        MessagePack.Formatters.Int32ArrayFormatter.Instance.Serialize(ref writer, value, options);
    }
}
