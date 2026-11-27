using System;
using System.Buffers;

namespace DataWF.Serialization;

public sealed class PooledBufferWriter : IBufferWriter<byte>, IDisposable
{
    private byte[] _buffer;
    private int _index;

    public PooledBufferWriter(int initialBufferSize = 8 * 1024)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(initialBufferSize);
    }

    public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _index);
    public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _index);

    public bool ReturnLater { get; private set; }

    public void Advance(int count) => _index += count;

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _buffer.AsMemory(_index);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _buffer.AsSpan(_index);
    }

    private void CheckAndResizeBuffer(int sizeHint)
    {
        int hint = sizeHint == 0 ? 1 : sizeHint;
        if (_index + hint > _buffer.Length)
        {
            int newSize = Math.Max(_buffer.Length * 2, _index + hint);
            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _index);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }
    }

    public ArraySegment<byte> GetBuffer()
    {
        ReturnLater = true;
        return new ArraySegment<byte>(_buffer, 0, _index);
    }

    public byte[] ToArray()
    {
        if (_index == 0)
        {
            return Array.Empty<byte>();
        }

        var result = new byte[_index];
        Buffer.BlockCopy(_buffer, 0, result, 0, _index);
        return result;
    }

    public void Dispose()
    {
        if (_buffer != null && !ReturnLater)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = null!;
        }
    }
}
