using MessagePack;

namespace DataWF.Serialization.Elements;

public class UInt16ArraySerializer : UnmanagedArraySerializer<ushort>
{
    public static readonly UInt16ArraySerializer Instance = new();

    public override void Read(ref MessagePackReader reader, scoped ref ushort[]? value, MessagePackSerializerOptions options)
    {
        value = MessagePack.Formatters.UInt16ArrayFormatter.Instance.Deserialize(ref reader, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref ushort[]? value, MessagePackSerializerOptions options)
    {
        MessagePack.Formatters.UInt16ArrayFormatter.Instance.Serialize(ref writer, value, options);
    }
}
