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
    public class DBComparerList : DBComparerList<DBItem>
    {
        public DBComparerList(params DBColumn[] columns) : base(columns)
        {
        }

        public DBComparerList(List<IComparer<DBItem>> comparers) : base(comparers)
        {
        }

        public DBComparerList(DBTable table, params string[] columns) : base(table, columns)
        {
        }

        public DBComparerList(DBSchema schema, string table, params string[] columns) : base(schema, table, columns)
        {
        }
    }

    public class DBComparerList<T> : IComparer, IComparer<T> where T : DBItem
    {
        protected List<IComparer<T>> comparers;

        public List<IComparer<T>> Comparers
        {
            get { return comparers; }
        }

        public DBComparerList(params DBColumn[] columns)
        {
            comparers = new List<IComparer<T>>();
            foreach (var col in columns)
                comparers.Add(col.CreateComparer<T>());
        }

        public DBComparerList(DBSchema schema, string table, params string[] columns)
            : this(schema.ParseTable(table), columns)
        {
        }

        public DBComparerList(DBTable table, params string[] columns)
        {
            comparers = new List<IComparer<T>>();
            foreach (string column in columns)
            {
                var dir = ListSortDirection.Ascending;
                if (column.EndsWith(" desc", StringComparison.OrdinalIgnoreCase))
                    dir = ListSortDirection.Descending;
                string name = column.Trim().IndexOf(" ", StringComparison.Ordinal) > 0
                                    ? column.Substring(0, column.IndexOf(" ", StringComparison.Ordinal))
                                    : column;
                var dbColumn = table.GetColumn(name);
                comparers.Add(dbColumn.CreateComparer<T>(dir));
            }
        }

        public DBComparerList(List<IComparer<T>> comparers)
        {
            this.comparers = comparers;
        }

        #region IComparer Members

        public int Compare(object x, object y)
        {
            return Compare(x as T, y as T);
        }

        #endregion

        #region IComparer<RowSetting> Members

        public int Compare(T x, T y)
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
            if (obj is DBComparerList<T> listComparer)
            {
                if (listComparer.comparers.Count == comparers.Count)
                {
                    for (int i = 0; i < comparers.Count; i++)
                    {
                        if (!listComparer.comparers[i].Equals(comparers[i]))
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
