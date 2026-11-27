using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace DataWF.Serialization;

internal class SequenceSegment : ReadOnlySequenceSegment<byte>
{
    public SequenceSegment(long index, byte[] buffer)
    {
        Buffer = buffer;
        Memory = buffer.AsMemory();
        RunningIndex = index;
    }

    public SequenceSegment(long index, byte[] buffer, int start, int count)
    {
        Buffer = buffer;
        Memory = buffer.AsMemory(start, count);
        RunningIndex = index;
    }

    public byte[] Buffer { get; set; }

    public bool ReturnLater { get; internal set; }

    public void SetNext(SequenceSegment segment)
    {
        Next = segment;
    }

    public ArraySegment<byte> GetPooled()
    {
        ReturnLater = true;
        if (MemoryMarshal.TryGetArray(Memory, out ArraySegment<byte> segment))
        {
            return segment;
        }
        return new ArraySegment<byte>(Buffer);
    }
}
