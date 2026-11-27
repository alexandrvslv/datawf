namespace DataWF.Comparers;

public sealed class UInt32NUniversalComparer : UniversalComparer<uint?>
{
    public static readonly UInt32NUniversalComparer Instance = new();

    public override bool DefaultEquality => true;

    public override bool DefaultOrder => true;

    public override int Compare(uint? x, uint? y)
    {
        if (x is null) return y is null ? 0 : -1;
        if (y is null) return 1;
        return x.GetValueOrDefault().CompareTo(y.GetValueOrDefault());
    }

    public override bool Equals(uint? x, uint? y) => x == y;

    public override int GetHashCode(uint? obj) => obj.GetValueOrDefault().GetHashCode();

}

