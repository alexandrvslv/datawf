using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Common
{
    public class ByteArrayComparer : IEqualityComparer<byte[]>, IEqualityComparer
    {
        public static readonly ByteArrayComparer Default = new ByteArrayComparer();
        public bool Equals(byte[] x, byte[] y)
        {
            if (x == y)
                return true;
            if (x == null || y == null || x.Length != y.Length)
                return false;
            return EqualsAsSpan(x, y);
        }

        public bool EqualsAsSpan(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
        {
            return x.SequenceEqual(y);
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            return Equals((byte[])x, (byte[])y);
        }

        public int GetHashCode(byte[] obj) => obj.GetHashCode();

        public int GetHashCode(object obj) => obj.GetHashCode();
    }
}

