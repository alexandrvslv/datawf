namespace DataWF.Comparers;

public sealed class Int32NUniversalComparer : UniversalComparer<int?>
{
    public static readonly UInt32UniversalComparer Instance = new();

    public override bool DefaultEquality => true;

    public override bool DefaultOrder => true;

    public override int Compare(int? x, int? y)
    {
        if (x is null) return y is null ? 0 : -1;
        if (y is null) return 1;
        return x.GetValueOrDefault().CompareTo(y.GetValueOrDefault());
    }

    public override bool Equals(int? x, int? y) => x == y;

    public override int GetHashCode(int? obj) => obj.GetValueOrDefault().GetHashCode();

}

