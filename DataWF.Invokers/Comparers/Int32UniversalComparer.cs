using System.Runtime.CompilerServices;

namespace DataWF.Comparers;

public sealed class Int32UniversalComparer : UniversalComparer<int>
{
    public static readonly Int32UniversalComparer Instance = new();

    public override bool DefaultEquality => true;

    public override bool DefaultOrder => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Compare(int x, int y) => x.CompareTo(y);

    public override bool Equals(int x, int y) => x == y;

    public override int GetHashCode(int obj) => obj.GetHashCode();

    public override bool Equals(int x, object? y) => y switch
    {
        int same => x == same,
        uint uintValue => x > -1 && (uint)x == uintValue,
        ushort ushortValue => x == ushortValue,
        short shortValue => x == shortValue,
        byte byteValue => x == byteValue,
        long longValue => x == longValue,
        ulong ulongValue => x > -1 && (ulong)x == ulongValue,
        _ => false
    };
}

