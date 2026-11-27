using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace DataWF.Serialization;

public class MessagePackInvokerSerializer : BaseSerializer
{
    public static MessagePackSerializerOptions DefaultOptions { get; } =
        MessagePackSerializerOptions.Standard.WithOmitAssemblyVersion(true);

    public MessagePackSerializerOptions Options { get; set; } = DefaultOptions;

    public void Serialize<T, TBuffer>(TBuffer buffer, in T? value)
        where TBuffer : IBufferWriter<byte>
    {
        var writer = new MessagePackWriter(buffer);
        try
        {
            GetSerializer<T>().Write(ref writer, ref Unsafe.AsRef(in value), Options);
            writer.Flush();
        }
        finally
        {
            //writeState.Reset();
        }
    }

    public override ArraySegment<byte> Serialize<T>(in T? value) where T : default
    {
        var buffer = new PooledBufferWriter();
        try
        {
            Serialize(buffer, in value);
            return buffer.ToArray();
        }
        finally
        {
            buffer.Dispose();
        }
    }

    public override ArraySegment<byte> SerializePooled<T>(in T? value) where T : default
    {
        var buffer = new PooledBufferWriter();
        try
        {
            Serialize(buffer, in value);
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
        Serialize(buffer, in value);
        buffer.Complete();
    }

    public override async ValueTask SerializeAsync<T>(
        Stream stream,
        T? value,
        CancellationToken cancellationToken = default) where T : default
    {
        var buffer = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));
        try
        {
            var writer = new MessagePackWriter(buffer);
            GetSerializer<T>().Write(ref writer, ref value, Options);
            writer.Flush();
            var result = await buffer.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (result.IsCanceled)
                throw new OperationCanceledException(cancellationToken);
        }
        finally
        {
            await buffer.CompleteAsync().ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    public override int Deserialize<T>(ArraySegment<byte> data, scoped ref T? value) where T : default
    {
        var memory = data.Array == null ? ReadOnlyMemory<byte>.Empty : data.AsMemory();
        return Deserialize(memory, ref value);
    }

    public override int Deserialize<T>(ReadOnlySpan<byte> data, scoped ref T? value) where T : default
        => Deserialize((ReadOnlyMemory<byte>)data.ToArray(), ref value);

    private int Deserialize<T>(ReadOnlyMemory<byte> data, scoped ref T? value)
    {
        var reader = new MessagePackReader(data);
        GetSerializer<T>().Read(ref reader, ref value, Options);
        return checked((int)reader.Consumed);
    }

    public override int Deserialize<T>(Stream stream, scoped ref T? value) where T : default
    {
        if (stream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var streamBuffer))
        {
            var data = streamBuffer.AsMemory(checked((int)memoryStream.Position));
            var bytesRead = Deserialize(data, ref value);
            memoryStream.Seek(bytesRead, SeekOrigin.Current);
            return bytesRead;
        }

        using var sequenceReader = new StreamSequenceReader(stream, bufferSize: StreamSequenceReader.DefaultBufferSize);
        while (sequenceReader.ReadMore())
        {
        }

        var reader = new MessagePackReader(sequenceReader.Sequence);
        GetSerializer<T>().Read(ref reader, ref value, Options);
        return checked((int)reader.Consumed);
    }

    public override async ValueTask<T?> DeserializeAsync<T>(
        Stream stream,
        T? value,
        CancellationToken cancellationToken = default) where T : default
    {
        if (stream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var streamBuffer))
        {
            var data = streamBuffer.AsMemory(checked((int)memoryStream.Position));
            var bytesRead = Deserialize(data, ref value);
            memoryStream.Seek(bytesRead, SeekOrigin.Current);
            return value;
        }

        if (stream.CanSeek)
        {
            var startPosition = stream.Position;
            var remainingLength = stream.Length - startPosition;
            if (remainingLength <= int.MaxValue)
            {
                if (remainingLength <= 0)
                    throw new EndOfStreamException("The stream does not contain a MessagePack value.");

                var buffer = ArrayPool<byte>.Shared.Rent((int)remainingLength);
                try
                {
                    var bytesRead = 0;
                    while (bytesRead < remainingLength)
                    {
                        var read = await stream.ReadAsync(
                            buffer.AsMemory(bytesRead, (int)remainingLength - bytesRead),
                            cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                        if (read == 0)
                            break;

                        bytesRead += read;
                    }

                    var contiguousReader = new MessagePackReader(buffer.AsMemory(0, bytesRead))
                    {
                        CancellationToken = cancellationToken,
                    };
                    GetSerializer<T>().Read(ref contiguousReader, ref value, Options);
                    stream.Position = startPosition + contiguousReader.Consumed;
                    return value;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        using var sequenceReader = new MessagePackStreamReader(stream, leaveOpen: true);
        var sequence = await sequenceReader.ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        if (sequence == null)
            throw new EndOfStreamException("The stream does not contain a MessagePack value.");

        var reader = new MessagePackReader(sequence.Value);
        GetSerializer<T>().Read(ref reader, ref value, Options);
        return value;
    }
}
