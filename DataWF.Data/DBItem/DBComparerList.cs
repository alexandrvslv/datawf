/*
 DBRowListComparer.cs
 
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Data
{
    public class DBComparerList : IComparer, IComparer<DBItem>
    {
        protected List<DBComparer> comparers;

        public List<DBComparer> Comparers
        {
            get { return comparers; }
        }

        public DBComparerList(params DBColumn[] columns)
        {
            comparers = new List<DBComparer>();
            foreach (var col in columns)
                comparers.Add(new DBComparer(col));
        }

        public DBComparerList(string table, params string[] columns)
            : this(DBService.ParseTable(table), columns)
        {
        }

        public DBComparerList(DBTable table, params string[] columns)
        {
            comparers = new List<DBComparer>();
            foreach (string column in columns)
            {
                var dir = ListSortDirection.Ascending;
                if (column.EndsWith(" desc", StringComparison.OrdinalIgnoreCase))
                    dir = ListSortDirection.Descending;
                string name = column.Trim().IndexOf(" ", StringComparison.Ordinal) > 0
                                    ? column.Substring(0, column.IndexOf(" ", StringComparison.Ordinal))
                                    : column;
                comparers.Add(new DBComparer(table, name, dir));
            }
        }

        public DBComparerList(List<DBComparer> comparers)
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
            foreach (DBComparer comparer in comparers)
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
