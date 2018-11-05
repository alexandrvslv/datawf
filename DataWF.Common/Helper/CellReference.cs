/*
 TemplateParcer.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Text.RegularExpressions;

namespace DataWF.Common
{
    public struct CellReference
    {
        public static readonly CellReference Empty;

        public static CellReference Parse(string reference)
        {
            reference = reference.Replace("$", "");
            MatchCollection mc = Regex.Matches(reference, @"\d[\d]*", RegexOptions.IgnoreCase);
            if (mc.Count == 1)
            {
                return new CellReference()
                {
                    Col = Helper.CharToInt(reference.Replace(mc[0].Value, "")),
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

        public int Col;

        public string ColA => Helper.IntToChar(Col);

        public int Row;

        public CellReference(int c, int r) : this()
        {
            Col = c;
            Row = r;
        }

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

        public CellReference Start;
        public CellReference End;

        public int Rows { get { return End.Row - Start.Row; } }

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