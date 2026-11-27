using MemoryPack;
using MemoryPack.Formatters;

namespace DataWF.Serialization.Elements;

public class UnmanagedArraySerializer<T> : ArraySerializer<T> where T : unmanaged
{

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T[]? value)
        => writer.WriteUnmanagedArray(value);

    public override void Read(ref MemoryPackReader reader, scoped ref T[]? value)
        => reader.ReadUnmanagedArray(ref value);
}



