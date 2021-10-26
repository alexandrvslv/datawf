using System.Text.RegularExpressions;

namespace DataWF.Common
{
    public struct CellReference
    {
        public static readonly CellReference Empty;

        public static CellReference Parse(string reference)
        {
            reference = reference.Replace("$", "");
            MatchCollection mc = Regex.Matches(reference, @"\d[\d]*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            if (mc.Count == 1)
            {
                return new CellReference()
                {
                    Col = Helper.CharToInt(reference.Substring(0, mc[0].Index)),
                    Row = int.Parse(mc[0].Value)
                };
            }
            return Empty;
        }

        public static bool operator ==(CellReference a, CellReference b)
        {
            return a.Col == b.Col && a.Row == b.Row;
        }

        public static bool operator !=(CellReference a, CellReference b)
        {
            return a.Col != b.Col || a.Row != b.Row;
        }

        public CellReference(int c, int r) : this()
        {
            Col = c;
            Row = r;
        }

        public int Col;

        public int Row;

        public string ColA => Helper.IntToChar(Col);


        public override string ToString()
        {
            return ColA + Row.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            var b = (CellReference)obj;
            return Col == b.Col && Row == b.Row;
        }

        public override int GetHashCode()
        {
            return Col ^ Row;
        }
    }
}