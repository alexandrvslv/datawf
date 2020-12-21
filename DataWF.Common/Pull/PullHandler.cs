using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DataWF.Common
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct PullHandler : IEquatable<PullHandler>, IComparable<PullHandler>
    {
        public static readonly PullHandler Zero = new PullHandler(0, 0);
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

        public static bool operator ==(PullHandler left, PullHandler right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PullHandler left, PullHandler right)
        {
            return !left.Equals(right);
        }

        public static explicit operator long(PullHandler pullBlock)
        {
            return pullBlock.Value;
        }

        public static explicit operator PullHandler(long handler)
        {
            return new PullHandler(handler);
        }

        public PullHandler(long handler)
        {
            Block = default(int);
            BlockIndex = default(int);
            Value = handler;
        }

        public PullHandler(int block, int blockIndex)
        {
            Value = default(long);
            Block = block;
            BlockIndex = blockIndex;
        }

        public int CompareTo(PullHandler other)
        {
            return Value.CompareTo(other.Value);
        }

        public int CompareTo(in PullHandler other)
        {
            return Value.CompareTo(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is PullHandler @ref && Equals(@ref);
        }

        public bool Equals(PullHandler other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public int GetSeqence(int blockSize)
        {
            return Block * blockSize + BlockIndex;
        }
    }
}
