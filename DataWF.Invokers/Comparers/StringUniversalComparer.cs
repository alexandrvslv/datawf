using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DataWF.Comparers;

public sealed class StringUniversalComparer : UniversalComparer<string>, IAlternateEqualityComparer<ReadOnlySpan<char>, string>
{
    public static readonly StringUniversalComparer Instance = new();
    private readonly StringComparison comparison;

    public override bool DefaultEquality => comparison == StringComparison.Ordinal;
    public override bool DefaultOrder => false;

    public StringUniversalComparer(StringComparison comparison = StringComparison.Ordinal)
    {
        this.comparison = comparison;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Compare(string? x, string? y) => string.Compare(x, y, comparison);

    public override bool Equals(string? x, string? y) => string.Equals(x, y, comparison);

    public override int GetHashCode(string obj) => obj.GetHashCode(comparison);

    public override bool Equals(string? x, object? y)
        => y switch
        {
            string same => string.Equals(x, same, comparison),
            null => x is null,
            _ => x is not null && string.Equals(x, y.ToString(), comparison)
        };

    public bool Equals(ReadOnlySpan<char> alternate, string other)
        => other is not null && alternate.Equals(other, comparison);

    public int GetHashCode(ReadOnlySpan<char> alternate)
        => string.GetHashCode(alternate, comparison);

    public string Create(ReadOnlySpan<char> alternate)
        => alternate.ToString();
}

