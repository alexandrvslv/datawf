using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Common
{
    public class ByteArrayComparer : IEqualityComparer<byte[]>, IEqualityComparer, IComparer<byte[]>
    {
        public static readonly ByteArrayComparer Default = new ByteArrayComparer();
        public bool Equals(byte[] x, byte[] y)
        {
            if (x == y)
                return true;
            if (x == null || y == null || x.Length != y.Length)
                return false;
            return Equals((ReadOnlySpan<byte>)x, (ReadOnlySpan<byte>)y);
        }

        public bool Equals(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
        {
            return x.SequenceEqual(y);
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            return Equals((byte[])x, (byte[])y);
        }

        public int GetHashCode(byte[] obj) => obj.GetHashCode();

        public int GetHashCode(ReadOnlySpan<byte> obj) => obj.GetHashCode();

        public int GetHashCode(object obj) => obj.GetHashCode();

        public int Compare(byte[] x, byte[] y)
        {
            if (x == null)
                return y == null ? 0 : -1;
            else if (y == null)
                return 1;
            return Compare((ReadOnlySpan<byte>)x, (ReadOnlySpan<byte>)y);
        }

        public int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
        {
            return x.SequenceCompareTo(y);
        }

        public int IndexOf(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
        {
            return x.IndexOf(y);
        }

        public bool EndWith(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
        {
            return x.EndsWith(y);
        }

        public bool StartsWith(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
        {
            return x.StartsWith(y);
        }

    }
}

