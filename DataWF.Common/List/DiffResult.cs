using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public enum DiffType
    {
        Deleted,
        Inserted
    }

    public class DiffResult
    {
        private readonly IList listA;
        private readonly IList listB;
        public DiffResult(IList listA, IList listB)
        {
            Length = 1;
            this.listA = listA;
            this.listB = listB;
        }
        public DiffType Type { get; set; }
        public int Index { get; set; }
        public int Length { get; set; }
        public string Result
        {
            get
            {
                string res = string.Empty;
                IList list = Type == DiffType.Deleted ? listA : listB;
                for (int i = Index; i < Index + Length; i++)
                {
                    res += list[i].ToString();
                }
                return res;
            }
        }

        public static List<DiffResult> Diff(string a, string b, bool upper = true)
        {
            string au = upper ? a.ToUpperInvariant() : a;
            string bu = upper ? b.ToUpperInvariant() : b;
            return Diff(au.ToCharArray(), bu.ToCharArray());
        }

        public static List<DiffResult> DiffLines(string a, string b)
        {
            return Diff(a.Split('\n'), b.Split('\n'));
        }

        public static void DiffTypeDef(IList a, IList b, int indexA, int indexB, out int newA, out int newB)
        {
            newA = a.Count;
            newB = b.Count;
            for (int i = indexA; i < a.Count; i++)
                for (int j = indexB; j < b.Count; j++)
                    if (a[i].Equals(b[j]))
                    {
                        if ((newA - indexA) + (newB - indexB) >= (i - indexA) + (j - indexB))
                        {
                            newA = i;
                            newB = j;
                            if (i > indexA)
                            {
                                return;
                            }
                        }
                        break;
                    }

        }

        public static List<DiffResult> Diff(IList a, IList b)
        {
            List<DiffResult> list = new List<DiffResult>();
            DiffResult curr = null;
            for (int i = 0, j = 0; i < a.Count || j < b.Count; i++, j++)
            {
                if (i >= a.Count)
                {
                    curr = new DiffResult(a, b) { Type = DiffType.Inserted, Index = j, Length = b.Count - j };
                    list.Add(curr);
                    break;
                }
                if (j >= b.Count)
                {
                    curr = new DiffResult(a, b) { Type = DiffType.Deleted, Index = i, Length = a.Count - i };
                    list.Add(curr);
                    break;
                }
                if (!a[i].Equals(b[j]))
                {
                    DiffTypeDef(a, b, i, j, out int newa, out int newb);
                    if (newa > i)
                    {
                        curr = new DiffResult(a, b) { Type = DiffType.Deleted, Index = i, Length = newa - i };
                        list.Add(curr);
                        i = newa;
                    }
                    if (newb > j)
                    {
                        curr = new DiffResult(a, b) { Type = DiffType.Inserted, Index = j, Length = newb - j };
                        list.Add(curr);
                        j = newb;
                    }
                }
            }
            return list;
        }
    }
}
