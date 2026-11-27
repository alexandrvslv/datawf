using DataWF.Comparers;
using System;

namespace DataWF;

public class Between<T> : IBetween<T>, IEquatable<Between<T>>
{
    public Between()
    {
        Min = default!;
        Max = default!;
    }

    public Between(T min, T max)
    {
        Min = min;
        Max = max;
    }

    public T Max { get; set; }

    public T Min { get; set; }

    public override bool Equals(object? obj) => Equals(obj as Between<T>);

    public bool Equals(Between<T>? other) => other is not null
        && UniversalComparer<T>.Default.Equals(Max, other.Max)
        && UniversalComparer<T>.Default.Equals(Min, other.Min);

    public override int GetHashCode() => HashCode.Combine(Max, Min);

    public object GetMax(object? context = null) => Max;

    public object GetMin(object? context = null) => Min;

    public static bool operator ==(Between<T> left, Between<T> right)
        => left?.Equals(right) ?? right is null;

    public static bool operator !=(Between<T> left, Between<T> right)
        => !(left == right);
}
