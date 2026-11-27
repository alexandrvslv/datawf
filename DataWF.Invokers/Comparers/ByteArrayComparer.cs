using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Comparers;

public sealed class ByteArrayComparer : UniversalComparer<byte[]>, IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]>
{
    public static readonly ByteArrayComparer Instance = new();

    public override bool DefaultEquality => true;

    public override bool DefaultOrder => true;

    public override bool Equals(byte[]? x, byte[]? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null || y == null || x.Length != y.Length)
            return false;
        return x.SequenceEqual(y);
    }

    public override int GetHashCode(byte[] obj) => GetHashCode(obj.AsSpan());

    public override int Compare(byte[]? x, byte[]? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        return x.SequenceCompareTo(y);
    }

    public bool Equals(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y) => x.SequenceEqual(y);

    public int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y) => x.SequenceCompareTo(y);

    public int GetHashCode(ReadOnlySpan<byte> obj)
    {
        var hash = new HashCode();
        hash.AddBytes(obj);
        return hash.ToHashCode();
    }

    public int IndexOf(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y) => x.IndexOf(y);

    public bool EndsWith(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y) => x.EndsWith(y);

    public bool StartsWith(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y) => x.StartsWith(y);

    public bool Equals(ReadOnlySpan<byte> alternate, byte[] other) => other is not null && alternate.SequenceEqual(other);

    public byte[] Create(ReadOnlySpan<byte> alternate) => alternate.ToArray();
}

