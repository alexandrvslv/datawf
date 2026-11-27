using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public class UnmanagedTypeInfoSerializer<T> : TypeInfoSerializer<T> where T : unmanaged
{

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T value)
    {
        writer.WriteUnmanaged(in value);
    }

    public override void Read(ref MemoryPackReader reader, scoped ref T value)
    {
        reader.ReadUnmanaged(out value);
    }
}
