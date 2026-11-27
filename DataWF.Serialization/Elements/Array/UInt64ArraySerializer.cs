using System;
using MessagePack;

namespace DataWF.Serialization.Elements;

public class UInt64ArraySerializer: UnmanagedArraySerializer<ulong>
{
    public static readonly UInt64ArraySerializer Instance = new();
    public override void Read(ref MessagePackReader reader, scoped ref ulong[]? value, MessagePackSerializerOptions options)
    {
        value = MessagePack.Formatters.UInt64ArrayFormatter.Instance.Deserialize(ref reader, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref ulong[]? value, MessagePackSerializerOptions options)
    {
        MessagePack.Formatters.UInt64ArrayFormatter.Instance.Serialize(ref writer, value, options);
    }
}
