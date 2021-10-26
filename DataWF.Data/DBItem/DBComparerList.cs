//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Data
{
    public class DBComparerList : IComparer, IComparer<DBItem>
    {
        protected List<IComparer> comparers;

        public List<IComparer> Comparers
        {
            get { return comparers; }
        }

        public DBComparerList(params DBColumn[] columns)
        {
            comparers = new List<IComparer>();
            foreach (var col in columns)
                comparers.Add(col.CreateComparer());
        }

        public DBComparerList(string table, params string[] columns)
            : this(DBService.Schems.ParseTable(table), columns)
        {
        }

        public DBComparerList(DBTable table, params string[] columns)
        {
            comparers = new List<IComparer>();
            foreach (string column in columns)
            {
                var dir = ListSortDirection.Ascending;
                if (column.EndsWith(" desc", StringComparison.OrdinalIgnoreCase))
                    dir = ListSortDirection.Descending;
                string name = column.Trim().IndexOf(" ", StringComparison.Ordinal) > 0
                                    ? column.Substring(0, column.IndexOf(" ", StringComparison.Ordinal))
                                    : column;
                var dbColumn = table.ParseColumn(name);
                comparers.Add(dbColumn.CreateComparer(dir));
            }
        }

        public DBComparerList(List<IComparer> comparers)
        {
            this.comparers = comparers;
        }

        #region IComparer Members

        public int Compare(object x, object y)
        {
            return Compare(x as DBItem, y as DBItem);
        }

        #endregion

        #region IComparer<RowSetting> Members

        public int Compare(DBItem x, DBItem y)
        {
            if ((x == null && y == null) || (x != null && x.Equals(y)))
                return 0;
            foreach (var comparer in comparers)
            {
                int retval = comparer.Compare(x, y);
                if (retval != 0)
                    return retval;
            }
            return x.GetHashCode().CompareTo(y.GetHashCode());
        }

        #endregion
        public override bool Equals(object obj)
        {
            if (obj is DBComparerList)
            {
                if (((DBComparerList)obj).comparers.Count == comparers.Count)
                {
                    for (int i = 0; i < comparers.Count; i++)
                    {
                        if (!((DBComparerList)obj).comparers[i].Equals(comparers[i]))
                            return false;
                    }
                    return true;
                }
                return false;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
