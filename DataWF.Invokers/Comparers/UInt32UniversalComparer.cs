using System.Runtime.CompilerServices;

namespace DataWF.Comparers;

public sealed class UInt32UniversalComparer : UniversalComparer<uint>
{
    public static readonly UInt32UniversalComparer Instance = new();

    public override bool DefaultEquality => true;

    public override bool DefaultOrder => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Compare(uint x, uint y) => x.CompareTo(y);

    public override bool Equals(uint x, uint y) => x == y;

    public override int GetHashCode(uint obj) => obj.GetHashCode();

    public override bool Equals(uint x, object? y) => y switch
    {
        uint same => x == same,
        int intValue => intValue >= 0 && x == (uint)intValue,
        ushort ushortValue => x == ushortValue,
        short shortValue => shortValue >= 0 && x == (uint)shortValue,
        byte byteValue => x == byteValue,
        long longValue => longValue >= 0 && x == (ulong)longValue,
        ulong ulongValue => x == ulongValue,
        _ => false
    };
}

