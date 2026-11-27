using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DataWF.Collections.Pulls;

[StructLayout(LayoutKind.Explicit)]
public readonly struct PullHandler : IEquatable<PullHandler>, IComparable<PullHandler>
{
    public static readonly PullHandler Zero = new(0, 0);

    public static PullHandler Max(PullHandler a, PullHandler b)
    {
        return a.Block > b.Block ? a
            : a.Block < b.Block ? b
            : a.BlockIndex >= b.BlockIndex ? a : b;
    }

    [FieldOffset(0)]
    public readonly int BlockIndex;
    [FieldOffset(4)]
    public readonly int Block;
    [FieldOffset(0)]
    public readonly long Value;

    public static PullHandler FromSeqence(int index, int blockSize)
    {
        return new PullHandler(index / blockSize, index % blockSize);
    }

    public static bool operator ==(PullHandler left, PullHandler right) => left.Equals(right);

    public static bool operator !=(PullHandler left, PullHandler right) => !left.Equals(right);

    public static explicit operator long(PullHandler pullBlock) => pullBlock.Value;

    public static explicit operator PullHandler(long handler) => new(handler);

    public PullHandler(long handler)
    {
        Block = default;
        BlockIndex = default;
        Value = handler;
    }

    public PullHandler(int block, int blockIndex)
    {
        Value = default;
        Block = block;
        BlockIndex = blockIndex;
    }

    public int CompareTo(PullHandler other) => Value.CompareTo(other.Value);

    public int CompareTo(in PullHandler other) => Value.CompareTo(other.Value);

    public override bool Equals(object? obj) => obj is PullHandler @ref && Equals(@ref);

    public bool Equals(PullHandler other) => Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public int GetSeqence(int blockSize) => Block * blockSize + BlockIndex;
}
