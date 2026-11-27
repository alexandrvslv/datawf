using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataWF.Serialization;

public static class StreamExtensions
{
    public static async ValueTask<ArraySegment<byte>> GetBytesAsync(this Stream stream, int bufferSize = 1024 * 4)
    {
        if (stream is MemoryStream memStream
        && memStream.TryGetBuffer(out var result))
            return result;

        var sequenceReader = new StreamSequenceReader(stream, bufferSize: bufferSize);
        while (await sequenceReader.ReadMoreAsync()) { }
        return sequenceReader.ToAraay();
    }

    public static ArraySegment<byte> GetBytes(this Stream stream, int bufferSize = 1024 * 4)
    {
        if (stream is MemoryStream memStream
        && memStream.TryGetBuffer(out var result))
            return result;

        var sequenceReader = new StreamSequenceReader(stream, bufferSize: bufferSize);
        while (sequenceReader.ReadMore()) { }
        return sequenceReader.ToAraay();
    }

    public static ArraySegment<byte> GetPooled(ReadOnlySpan<byte> bytes)
    {
        var rented = ArrayPool<byte>.Shared.Rent(bytes.Length);
        bytes.CopyTo(rented);
        return new ArraySegment<byte>(rented, 0, bytes.Length);
    }

    public static ArraySegment<byte> GetPooled(ReadOnlySequence<byte> bytes)
    {
        var rented = ArrayPool<byte>.Shared.Rent((int)bytes.Length);
        bytes.CopyTo(rented);
        return new ArraySegment<byte>(rented, 0, (int)bytes.Length);
    }

    public static async ValueTask<ArraySegment<byte>> GetPooledBytesAsync(this Stream stream, int bufferSize = 1024 * 4)
    {
        if (stream is MemoryStream memStream
        && memStream.TryGetBuffer(out var result))
            return GetPooled(result);

        var sequenceReader = new StreamSequenceReader(stream, bufferSize: bufferSize);
        while (await sequenceReader.ReadMoreAsync()) { }
        return sequenceReader.Count switch
        {
            0 => ArraySegment<byte>.Empty,
            1 => sequenceReader.GetPooledFirstBuffer(),
            _ => GetPooled(sequenceReader.Sequence),
        };
    }

    public static ArraySegment<byte> GetPooledBytes(this Stream stream, int bufferSize = 1024 * 4)
    {
        if (stream is MemoryStream memStream
        && memStream.TryGetBuffer(out var result))
            return GetPooled(result);

        var sequenceReader = new StreamSequenceReader(stream, bufferSize: bufferSize);
        while (sequenceReader.ReadMore()) { }
        return sequenceReader.Count switch
        {
            0 => ArraySegment<byte>.Empty,
            1 => sequenceReader.GetPooledFirstBuffer(),
            _ => GetPooled(sequenceReader.Sequence),
        };
    }

    public static void WriteCommentCount(this Utf8JsonWriter writer, int value)
    {
        Span<char> buffer = stackalloc char[11];
        if (value.TryFormat(buffer, out var writtenChars))
            writer.WriteCommentValue(buffer[..writtenChars]);
    }



}
