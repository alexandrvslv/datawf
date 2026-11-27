using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Serialization;

public sealed class PooledMultiBufferWriter : IBufferWriter<byte>, IDisposable
{
    private SequenceSegment? _firstSegment;
    private SequenceSegment? _lastSegment;
    private int _count = 0;
    private int _bufferSize;
    private ArrayPool<byte> _pool;
    private int _bufferIndex;
    private byte[] _buffer;
    private int _index;

    public PooledMultiBufferWriter(int bufferSize = 16 * 1024)
    {
        _bufferSize = bufferSize;
        _pool = ArrayPool<byte>.Shared;
        _buffer = _pool.Rent(_bufferSize);
    }

    public int Count => _count + (_bufferIndex > 0 ? 1 : 0);

    public ReadOnlySpan<byte> BufferSpan => _buffer.AsSpan(0, _bufferIndex);

    public ReadOnlySequence<byte> Sequence
    {
        get
        {
            if (_firstSegment == null)
                return _bufferIndex > 0
                ? new ReadOnlySequence<byte>(_buffer, 0, _bufferIndex)
                : ReadOnlySequence<byte>.Empty;
            CheckSegments();
            return new ReadOnlySequence<byte>(_firstSegment, 0, _lastSegment!, _lastSegment!.Memory.Length);
        }
    }

    private void CheckSegments()
    {
        if (_lastSegment != null && _lastSegment.Buffer != _buffer)
        {
            var segment = new SequenceSegment(_index, _buffer, 0, _bufferIndex);
            _lastSegment.SetNext(segment);
            _lastSegment = segment;
            _index += _bufferIndex;
            _count++;
        }
    }

    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (_firstSegment is not { } segment)
        {
            await stream.WriteAsync(_buffer, 0, _bufferIndex, cancellationToken);
            return;
        }
        CheckSegments();
        while (segment != null)
        {
            await stream.WriteAsync(segment.Memory, cancellationToken);
            segment = segment.Next as SequenceSegment;
        }
    }

    public void Advance(int count)
    {
        if (_bufferIndex + count > _buffer.Length)
            throw new InvalidOperationException();
        _bufferIndex += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _buffer.AsMemory(_bufferIndex);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _buffer.AsSpan(_bufferIndex);
    }

    private void CheckAndResizeBuffer(int sizeHint)
    {
        int hint = sizeHint == 0 ? 1 : sizeHint;
        if (_bufferIndex + hint > _buffer.Length)
        {
            var segment = new SequenceSegment(_index, _buffer, 0, _bufferIndex);
            _index += _bufferIndex;
            _count++;
            _bufferIndex = 0;
            if (_firstSegment == null)
            {
                _firstSegment = _lastSegment = segment;
            }
            else
            {
                _lastSegment!.SetNext(segment);
                _lastSegment = segment;
            }
            _buffer = ArrayPool<byte>.Shared.Rent(Math.Max(_bufferIndex, sizeHint));
        }
    }

    public void Dispose()
    {
        Reset();
    }

    public void Reset()
    {
        var segment = _firstSegment;
        byte[]? buffer = null;
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
        if (_buffer != null && (buffer == null || buffer != _buffer))
            _pool.Return(_buffer);
        _index = 0;
        _count = 0;
        _bufferIndex = 0;
        _buffer = null!;
        _lastSegment = null;
        _firstSegment = null;
    }

    public ArraySegment<byte> ToAraay()
    {
        switch (Count)
        {
            case 0: return ArraySegment<byte>.Empty;
            case 1: return BufferSpan.ToArray();
            default: return Sequence.ToArray();
        }
    }

    public ArraySegment<byte> GetPulled()
    {
        return Count switch
        {
            0 => ArraySegment<byte>.Empty,
            _ => StreamExtensions.GetPooled(Sequence),
        };
    }

}
