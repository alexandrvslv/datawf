using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MemoryPack;
using MemoryPack.Internal;

namespace DataWF.Serialization;

public class MemoryPackInvokerSerializer : BaseSerializer
{
    [ThreadStatic]
    private static MemoryPackReaderOptionalState? readState;

    [ThreadStatic]
    private static MemoryPackWriterOptionalState? writeState;

    [ThreadStatic]
    private static ReusableLinkedArrayBufferWriter? writeBuffer;

    public bool Indent { get; set; } = false;

    public void Serialize<T, TBuffer>(TBuffer buffer, in T? value)
        where TBuffer : IBufferWriter<byte>
    {
        writeState ??= MemoryPackWriterOptionalStatePool.Rent(MemoryPackSerializerOptions.Default);
        var writer = buffer is ReusableLinkedArrayBufferWriter reusableBuffer
            ? new MemoryPackWriter<TBuffer>(ref buffer, reusableBuffer.DangerousGetFirstBuffer(), writeState)
            : new MemoryPackWriter<TBuffer>(ref buffer, writeState);
        SerializeWithWriter(ref writer, in value);
    }

    private void SerializeWithWriter<T, TBuffer>(ref MemoryPackWriter<TBuffer> writer, in T? value)
        where TBuffer : IBufferWriter<byte>
    {
        try
        {
            GetSerializer<T>().Write(ref writer, ref Unsafe.AsRef(in value));
            writer.Flush();
        }
        finally
        {
            //writeState.Reset();
        }
    }

    public override ArraySegment<byte> Serialize<T>(in T? value) where T : default
    {
        var buffer = writeBuffer ??= new ReusableLinkedArrayBufferWriter(useFirstBuffer: true, pinned: true);
        try
        {
            Serialize(buffer, value);
            return buffer.ToArrayAndReset();
        }
        finally
        {
            buffer.Reset();
        }
    }

    public override ArraySegment<byte> SerializePooled<T>(in T? value) where T : default
    {
        var buffer = new PooledBufferWriter();
        try
        {
            Serialize(buffer, value);
            return buffer.GetBuffer();
        }
        finally
        {
            buffer.Dispose();
        }
    }

    public override void Serialize<T>(Stream stream, in T? value) where T : default
    {
        var buffer = PipeWriter.Create(stream);
        Serialize(buffer, value);
        buffer.Complete();
    }

    public override async ValueTask SerializeAsync<T>(Stream stream, T? value, CancellationToken cancellationToken = default) where T : default
    {
        var buffer = ReusableLinkedArrayBufferWriterPool.Rent();
        try
        {
            Serialize(buffer, in value);
            await buffer.WriteToAndResetAsync(stream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }
        finally
        {
            ReusableLinkedArrayBufferWriterPool.Return(buffer);
        }
    }

    public override int Deserialize<T>(ArraySegment<byte> data, scoped ref T? value) where T : default
        => Deserialize(data.AsSpan(), ref value);

    public override int Deserialize<T>(ReadOnlySpan<byte> data, scoped ref T? value) where T : default
    {
        readState ??= MemoryPackReaderOptionalStatePool.Rent(MemoryPackSerializerOptions.Default);
        var reader = new MemoryPackReader(data, readState);
        try
        {
            GetSerializer<T>().Read(ref reader, ref value);
            return reader.Consumed;
        }
        finally
        {
            reader.Dispose();
            //readState.Reset();
        }
    }

    public int Deserialize<T>(in ReadOnlySequence<byte> sequence, scoped ref T? value)
    {
        readState ??= MemoryPackReaderOptionalStatePool.Rent(MemoryPackSerializerOptions.Default);
        var reader = new MemoryPackReader(sequence, readState);
        try
        {
            GetSerializer<T>().Read(ref reader, ref value);
            return reader.Consumed;
        }
        finally
        {
            reader.Dispose();
            //readState.Reset();
        }
    }

    public override int Deserialize<T>(Stream stream, scoped ref T? value) where T : default
    {
        if (stream is MemoryStream ms && ms.TryGetBuffer(out var streamBuffer))
        {
            var bytesRead = Deserialize<T>(streamBuffer.AsSpan(checked((int)ms.Position)), ref value);

            ms.Seek(bytesRead, SeekOrigin.Current);
            return bytesRead;
        }
        using var sequenceReader = new StreamSequenceReader(stream, bufferSize: StreamSequenceReader.DefaultBufferSize);
        while (sequenceReader.ReadMore())
        {
            // TODO chunck/chain reading
        }
        return sequenceReader.Count switch
        {
            0 => 0,
            1 => Deserialize(sequenceReader.FirstSpan, ref value),
            _ => Deserialize(sequenceReader.Sequence, ref value),
        };
    }

    public override async ValueTask<T?> DeserializeAsync<T>(Stream stream, T? value, CancellationToken cancellationToken = default) where T : default
    {
        if (stream is MemoryStream ms && ms.TryGetBuffer(out var streamBuffer))
        {
            var bytesRead = Deserialize<T>(streamBuffer.AsSpan(checked((int)ms.Position)), ref value);

            ms.Seek(bytesRead, SeekOrigin.Current);
            return value;
        }
        using var sequenceReader = new StreamSequenceReader(stream, bufferSize: StreamSequenceReader.DefaultBufferSize);
        while (await sequenceReader.ReadMoreAsync())
        {
            // TODO chunck/chain reading
        }
        switch (sequenceReader.Count)
        {
            case 0:
                return value;
            case 1:
                Deserialize(sequenceReader.FirstSpan, ref value);
                break;
            default:
                Deserialize(sequenceReader.Sequence, ref value);
                break;
        }
        return value;
    }




}
