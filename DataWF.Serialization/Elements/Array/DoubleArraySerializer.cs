using MessagePack;

namespace DataWF.Serialization.Elements;

public class DoubleArraySerializer : UnmanagedArraySerializer<double>
{
    public static readonly DoubleArraySerializer Instance = new();

    public override void Read(ref MessagePackReader reader, scoped ref double[]? value, MessagePackSerializerOptions options)
    {
        value = MessagePack.Formatters.DoubleArrayFormatter.Instance.Deserialize(ref reader, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref double[]? value, MessagePackSerializerOptions options)
    {
        MessagePack.Formatters.DoubleArrayFormatter.Instance.Serialize(ref writer, value, options);
    }
}
