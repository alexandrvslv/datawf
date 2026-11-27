using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace DataWF.Serialization;

public class StreamSequenceReader : IDisposable
{
    private const int MinimumSegmentSize = 4 * 1024;
    internal const int DefaultBufferSize = 64 * 1024;
    private readonly int _bufferSize;
    private readonly Stream _stream;
    private readonly bool _ownsStream;
    private readonly ArrayPool<byte> _pool;
    private bool _finished;
    private long _index;
    private int _count;
    private SequenceSegment? _firstSegment;
    private SequenceSegment? _lastSegment;
    private byte[]? _buffer;
    private int _bufferIndex;

    public StreamSequenceReader(Stream stream, bool ownsStream = false, int bufferSize = MinimumSegmentSize)
    {
        _bufferSize = bufferSize;
        _stream = stream;
        _ownsStream = ownsStream;
        _pool = ArrayPool<byte>.Shared;
    }

    public ReadOnlySequence<byte> Sequence
    {
        get
        {
            if (_firstSegment == null)
                return ReadOnlySequence<byte>.Empty;

            return new ReadOnlySequence<byte>(_firstSegment, 0, _lastSegment!, _lastSegment!.Memory.Length);
        }
    }

    public int Count => _count;

    public ReadOnlySpan<byte> FirstSpan => _firstSegment!.Memory.Span;

    public ReadOnlyMemory<byte> FirstMemory => _firstSegment!.Memory;

    public async ValueTask<bool> ReadMoreAsync()
    {
        if (_finished)
            return false;

        if (_buffer == null || _bufferIndex == _buffer.Length)
        {
            _buffer = _pool.Rent(_bufferSize);
            _bufferIndex = 0;
        }

        var bytesRead = await _stream.ReadAsync(_buffer.AsMemory(_bufferIndex, _buffer.Length - _bufferIndex));
        if (bytesRead == 0)
        {
            if (_bufferIndex == 0)
                _pool.Return(_buffer);
            _finished = true;
            return false;
        }
        var segment = new SequenceSegment(_index, _buffer, _bufferIndex, bytesRead);
        _bufferIndex += bytesRead;
        _index += bytesRead;
        _count += 1;
        if (_firstSegment == null)
        {
            _firstSegment = _lastSegment = segment;
        }
        else
        {
            _lastSegment!.SetNext(segment);
            _lastSegment = segment;
        }
        return true;
    }

    public bool ReadMore()
    {
        if (_finished)
            return false;

        if (_buffer == null || _bufferIndex == _buffer.Length)
        {
            _buffer = _pool.Rent(_bufferSize);
            _bufferIndex = 0;
        }

        var bytesRead = _stream.Read(_buffer, _bufferIndex, _buffer.Length - _bufferIndex);
        if (bytesRead == 0)
        {
            _finished = true;
            return false;
        }
        var segment = new SequenceSegment(_index, _buffer, _bufferIndex, bytesRead);
        _bufferIndex += bytesRead;
        _index += bytesRead;
        _count += 1;
        if (_firstSegment == null)
        {
            _firstSegment = segment;
            _lastSegment = segment;
        }
        else
        {
            _lastSegment!.SetNext(segment);
            _lastSegment = segment;
        }
        return true;
    }

    public void Reset()
    {
        var segment = _firstSegment;
        var buffer = (byte[]?)null;
        while (segment != null)
        {
            if (buffer != segment.Buffer)
            {
                if (!segment.ReturnLater)
                    _pool.Return(segment.Buffer);
                buffer = segment.Buffer;
            }
            segment = segment.Next as SequenceSegment;
        }
        _index = 0;
        _count = 0;
        _bufferIndex = 0;
        _buffer = null;
        _lastSegment = null;
        _firstSegment = null;
        _finished = false;
    }

    public ArraySegment<byte> GetPooledFirstBuffer()
    {
        return _firstSegment?.GetPooled() ?? ArraySegment<byte>.Empty;
    }

    public void Dispose()
    {
        Reset();

        if (_ownsStream)
        {
            _stream.Dispose();
        }
    }

    public ArraySegment<byte> ToAraay()
    {
        switch (Count)
        {
            case 0: return ArraySegment<byte>.Empty;
            case 1: return FirstSpan.ToArray();
            default: return Sequence.ToArray();
        }
    }

    public ArraySegment<byte> GetPulled()
    {
        return Count switch
        {
            0 => ArraySegment<byte>.Empty,
            1 => GetPooledFirstBuffer(),
            _ => StreamExtensions.GetPooled(Sequence),
        };
    }
}
