using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Comparers;

public sealed class DateTimePartComparer : UniversalComparer<DateTime>
{
    public static readonly DateTimePartComparer Instance = new();

    public override bool DefaultEquality => false;

    public override bool DefaultOrder => false;

    public override bool Equals(DateTime x, DateTime y)
    {
        if (x.TimeOfDay == TimeSpan.Zero || y.TimeOfDay == TimeSpan.Zero)
            return x.Date.Equals(y.Date);
        return x == y;
    }

    public override int Compare(DateTime x, DateTime y)
    {
        if (x.TimeOfDay == TimeSpan.Zero || y.TimeOfDay == TimeSpan.Zero)
            return x.Date.CompareTo(y.Date);
        return x.CompareTo(y);
    }

    public override int GetHashCode(DateTime obj) => obj.Date.GetHashCode();

    public override bool Equals(DateTime x, object? y)
        => y switch
        {
            DateTime dt => Equals(x, dt),
            DateTimeOffset dto => Equals(x, dto.DateTime),
            DateOnly dateOnly => Equals(x, dateOnly.ToDateTime(TimeOnly.MinValue)),
            null => Equals(x, DateTime.MinValue),
            _ => false
        };
}
public sealed class NDateTimePartComparer : UniversalComparer<DateTime?>
{
    public static readonly new NDateTimePartComparer Default = new();

    public override bool DefaultEquality => false;

    public override bool DefaultOrder => false;

    public override int Compare(DateTime? x, DateTime? y)
    {
        if (x is null) return y is null ? 0 : -1;
        if (y is null) return 1;
        return DateTimePartComparer.Default.Compare(x.Value, y.Value);
    }

    public override bool Equals(DateTime? x, DateTime? y)
    {
        if (x is null) return y is null;
        if (y is null) return false;
        return DateTimePartComparer.Default.Equals(x.Value, y.Value);
    }

    public override int GetHashCode(DateTime? obj) => obj.GetHashCode();

}

