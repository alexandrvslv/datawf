using System;
using System.Runtime.CompilerServices;
using MessagePack;
using MessagePack.Resolvers;
using MemoryPack;
using MemoryPack.Formatters;

namespace DataWF.Serialization.Elements;

public abstract class UnmanagedSerializer<T> : ElementSerializer<T> where T : unmanaged
{
    protected UnmanagedSerializer()
    {
    }

    protected UnmanagedSerializer(ISerializeTypeInfo<T> typeInfo) : base(typeInfo)
    {
    }

    public override void Read(ref MemoryPackReader reader, scoped ref T value)
    {
        reader.ReadUnmanaged(out value);
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T value)
    {
        writer.WriteUnmanaged(value);
    }    
}
