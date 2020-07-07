namespace DataWF.Common
{
    public struct CellRange
    {
        public static CellRange Parse(string reference)
        {
            var range = new CellRange();

            string[] split1 = reference.Split(':');
            range.Start = CellReference.Parse(split1[0]);

            if (split1.Length > 1)
                range.End = CellReference.Parse(split1[1]);
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

        public int Rows
        {
            get => End.Row - Start.Row;
        }

        public override string ToString()
        {
            return $"{Start}:{End}";
        }

        public bool Intersect(CellRange r)
        {
            return !((Start.Col >= r.End.Col) || (End.Col <= r.Start.Col) ||
                    (Start.Row >= r.End.Row) || (End.Row <= r.Start.Row));
        }
    }
}