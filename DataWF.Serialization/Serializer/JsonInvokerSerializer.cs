using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Serialization;

public class JsonInvokerSerializer : BaseSerializer
{
    public JsonInvokerSerializer()
    { }

    public bool Indented
    {
        get => Options.WriteIndented;
        set => Options.WriteIndented = value;
    }

    public JsonIgnoreCondition DefaultIgnoreCondition
    {
        get => Options.DefaultIgnoreCondition;
        set => Options.DefaultIgnoreCondition = value;
    }

    public JsonSerializerOptions Options = new JsonSerializerOptions()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private void Serialize<TBuffer, T>(TBuffer buffer, in T? value)
        where TBuffer : IBufferWriter<byte>
    {
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions
        {
            Indented = Options.WriteIndented,
        });
        GetSerializer<T>().Write(writer, ref Unsafe.AsRef(in value), Options);
        writer.Flush();
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

    public override async ValueTask SerializeAsync<T>(Stream stream, T? value, CancellationToken cancellationToken = default) where T : default
    {
        var buffer = new PooledMultiBufferWriter();
        try
        {
            Serialize(buffer, in value);
            await buffer.WriteToAsync(stream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }
        finally
        {
            buffer.Dispose();
        }
    }

    public override int Deserialize<T>(ArraySegment<byte> data, scoped ref T? value) where T : default
        => Deserialize(data.AsSpan(), ref value);

    public override int Deserialize<T>(ReadOnlySpan<byte> data, scoped ref T? value) where T : default
    {
        var reader = new Utf8JsonReader(data, new JsonReaderOptions
        {
            AllowTrailingCommas = Options.AllowTrailingCommas,
            CommentHandling = Options.ReadCommentHandling,
        });
        reader.Read();
        GetSerializer<T>().Read(ref reader, ref value, Options);
        return (int)reader.BytesConsumed;
    }

    public int Deserialize<T>(in ReadOnlySequence<byte> data, scoped ref T? value)
    {
        var reader = new Utf8JsonReader(data, new JsonReaderOptions
        {
            AllowTrailingCommas = Options.AllowTrailingCommas,
            CommentHandling = Options.ReadCommentHandling,
        });
        reader.Read();
        GetSerializer<T>().Read(ref reader, ref value, Options);
        return (int)reader.BytesConsumed;
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
                return value!;
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
