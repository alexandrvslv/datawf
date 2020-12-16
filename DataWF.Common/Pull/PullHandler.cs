using System;
using System.Runtime.InteropServices;

namespace DataWF.Common
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct PullHandler : IEquatable<PullHandler>, IComparable<PullHandler>
    {
        public static readonly PullHandler Zero = new PullHandler(0, 0);
        [FieldOffset(2)]
        public readonly short Block;
        [FieldOffset(0)]
        public readonly short BlockIndex;
        [FieldOffset(0)]
        public readonly int Handler;

        public static PullHandler FromSeqence(int index, int blockSize)
        {
            return new PullHandler((short)(index / blockSize), (short)(index % blockSize));
        }

        public static bool operator ==(PullHandler left, PullHandler right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PullHandler left, PullHandler right)
        {
            return !left.Equals(right);
        }

        public static explicit operator int(PullHandler pullBlock)
        {
            return pullBlock.Handler;
        }

        public static explicit operator PullHandler(int handler)
        {
            return new PullHandler(handler);
        }

        public PullHandler(int handler)
        {
            Block = 0;
            BlockIndex = 0;
            Handler = handler;
        }

        public PullHandler(short block, short blockIndex)
        {
            Handler = 0;
            Block = block;
            BlockIndex = blockIndex;
        }

        public int CompareTo(PullHandler other)
        {
            return Handler.CompareTo(other.Handler);
        }

        public int CompareTo(in PullHandler other)
        {
            return Handler.CompareTo(other.Handler);
        }

        public override bool Equals(object obj)
        {
            return obj is PullHandler @ref && Equals(@ref);
        }

        public bool Equals(PullHandler other)
        {
            return Handler == other.Handler;
        }

        public override int GetHashCode()
        {
            return Handler;
        }

        public int GetSeqence(int blockSize)
        {
            return Block * blockSize + BlockIndex;
        }
    }
}
