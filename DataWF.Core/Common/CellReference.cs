using System;
using System.Text;
using System.Text.RegularExpressions;

namespace DataWF;

public partial struct CellReference : IEquatable<CellReference>
{
    public static readonly CellReference Empty;

    [GeneratedRegex(@"\d[\d]*", RegexOptions.IgnoreCase | RegexOptions.Compiled, "ru-RU")]
    private static partial Regex DDRegex();

    //https://stackoverflow.com/a/182924/4682355
    public static string IntToAbc(int val)
    {
        var sb = new StringBuilder();
        val += 1;

        while (val > 0)
        {
            var mod = (val - 1) % 26;
            sb.Insert(0, (char)((int)'A' + mod));
            val = (val - mod) / 26;
        }

        return sb.ToString();
    }

    public static int AbcToInt(ReadOnlySpan<char> val)
    {
        int rez = 0;
        var diff = (int)'A' - 1;
        for (var i = 0; i < val.Length; i++)
        {
            rez += ((int)val[i] - diff) * (int)Math.Pow(26, val.Length - (i + 1));
        }
        return rez - 1;
    }

    public static CellReference Parse(ReadOnlySpan<char> reference)
    {
        var index = reference.IndexOf('$');
        reference = index >= 0 ? reference[(index + 1)..] : reference;
        foreach (var match in DDRegex().EnumerateMatches(reference))
        {
            return new CellReference()
            {
                Col = AbcToInt(reference[..match.Index]),
                Row = int.Parse(reference.Slice(match.Index, match.Length))
            };
        }
        return Empty;
    }

    public static bool operator ==(CellReference a, CellReference b)
        => a.Col == b.Col && a.Row == b.Row;

    public static bool operator !=(CellReference a, CellReference b)
        => a.Col != b.Col || a.Row != b.Row;

    public CellReference(int c, int r) : this()
    {
        Col = c;
        Row = r;
    }

    public int Col;

    public int Row;

    public string ColA => IntToAbc(Col);

    public override string ToString()
    {
        return ColA + Row.ToString();
    }

    public override bool Equals(object? obj)
        => Equals(obj as CellReference?);

    public bool Equals(CellReference? other)
        => other is CellReference cellReference
            && Col == cellReference.Col
            && Row == cellReference.Row;

    public bool Equals(CellReference other)
        => Col == other.Col && Row == other.Row;

    public override int GetHashCode()
        => HashCode.Combine(Col, Row);

}
