using System;

namespace DataWF;

public struct CellRange: IEquatable<CellRange>
{
    public static CellRange Parse(ReadOnlySpan<char> reference)
    {
        var range = new CellRange();
        var index = 0;
        foreach (var part in reference.Split(':'))
        {
            if (index == 0)
                range.Start = CellReference.Parse(reference[part]);
            else if (index == 1)
                range.End = CellReference.Parse(reference[part]);
            index++;
        }
        return range;
    }

    public CellRange(int c1, int r1, int c2, int r2) : this()
    {
        Start = new CellReference(c1, r1);
        End = new CellReference(c2, r2);
    }

    public CellRange(CellReference sref1, CellReference sref2) : this()
    {
        Start = sref1;
        End = sref2;
    }

    public CellReference Start;

    public CellReference End;

    public int Rows => End.Row - Start.Row;

    public override string ToString()
    {
        return $"{Start}:{End}";
    }

    public bool Intersect(CellRange r)
    {
        return !((Start.Col >= r.End.Col) || (End.Col <= r.Start.Col) ||
                (Start.Row >= r.End.Row) || (End.Row <= r.Start.Row));
    }

    public bool Equals(CellRange other)
    {
        return Start.Equals(other.Start) && End.Equals(other.End);
    }
}
