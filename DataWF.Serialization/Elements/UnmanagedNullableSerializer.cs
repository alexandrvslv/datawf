using MemoryPack;
using MemoryPack.Formatters;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public class UnmanagedNullableSerializer<T> : NullableSerializer<T> where T : unmanaged
{
    public static readonly new UnmanagedNullableSerializer<T> Instance = new();

    public UnmanagedNullableSerializer(IElementSerializer<T>? valueSerializer) : base(valueSerializer)
    { }

    public UnmanagedNullableSerializer() : base()
    { }

    public override bool IsAttributeSerializer => true;

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T? value)
    {
        writer.DangerousWriteUnmanaged(in value);
    }

    public override void Read(ref MemoryPackReader reader, scoped ref T? value)
    {
        reader.DangerousReadUnmanaged(out value);
    }
}
