/*
 DBRowComparer.cs
 
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
using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Data
{
    public class DBComparer : DBComparer<DBItem>
    {
        public DBComparer(DBColumn column, ListSortDirection direction = ListSortDirection.Ascending)
            : base(column, direction)
        { }

        public DBComparer(string table, string column, ListSortDirection direction = ListSortDirection.Ascending)
            : base(table, column, direction)
        { }

        public DBComparer(DBTable table, string column, ListSortDirection direction = ListSortDirection.Ascending)
            : base(table, column, direction)
        { }
    }

    public class DBComparer<T> : IComparer<T>, IComparer where T : DBItem
    {
        public DBTable Table;
        public string PropertyName;
        public string Format;
        public ListSortDirection Direction;
        public CompareType Comparer;
        public object Value;
        private DBColumn property;
        public readonly bool buffered;
        private readonly bool refernce;
        public bool Hash;

        public DBComparer(DBTable table, DBColumn column, string proeprty, ListSortDirection direction = ListSortDirection.Ascending)
        {
            Table = table;
            PropertyName = proeprty;
            Direction = direction;
            property = column;
            buffered = property != null && property.Name == PropertyName;
            refernce = property != null && property.IsReference;
        }

        public DBComparer(DBColumn column, ListSortDirection direction = ListSortDirection.Ascending)
            : this(column.Table, column, column.Name, direction)
        { }

        public DBComparer(string table, string column, ListSortDirection direction = ListSortDirection.Ascending)
            : this(DBService.Schems.ParseTable(table), column, direction)
        { }

        public DBComparer(DBTable table, string column, ListSortDirection direction = ListSortDirection.Ascending)
            : this(table, table.ParseColumn(column), column, direction)
        { }

        #region IComparer
        public int Compare(T x, object key)
        {
            object xo = buffered ? x.GetValue(property) : x[PropertyName];
            if (Format != null && property != null && property.DataType == typeof(DateTime) && xo != null && key != null && key is DateTime)
            {//TODO datetime check only date need sone new attribute to check time
                return string.Compare(((DateTime)xo).ToString(Format), ((DateTime)key).ToString(Format), StringComparison.Ordinal);
            }
            return ListHelper.Compare(xo, key, null, Direction, false);
        }

        public bool Predicate(T x)
        {
            int rez = Compare(x, Value);
            bool f = false;
            switch (Comparer.Type)
            {
                case CompareTypes.Equal:
                    f = rez == 0 ? !Comparer.Not : Comparer.Not;
                    break;
                case CompareTypes.Greater:
                    f = rez > 0;
                    break;
                case CompareTypes.GreaterOrEqual:
                    f = rez >= 0;
                    break;
                case CompareTypes.Less:
                    f = rez < 0;
                    break;
                case CompareTypes.LessOrEqual:
                    f = rez <= 0;
                    break;
                case CompareTypes.In:
                    break;
                case CompareTypes.Like:
                    break;
                case CompareTypes.Between:
                    break;
                case CompareTypes.Is:
                    break;
                case CompareTypes.As:
                    break;
                case CompareTypes.Using:
                    break;
            }
            return f;
        }

        public int Compare(T x, T y)
        {
            int rez = 0;
            if (x == null)
                rez = (y == null) ? 0 : -1;
            else if (y == null)
                rez = 1;
            else if (x.Equals(y))
                rez = 0;
            else
            {
                object xValue, yValue;
                if (refernce)
                {
                    xValue = buffered ? x.GetRef(property) : x.GetRef(PropertyName);
                    yValue = buffered ? y.GetRef(property) : y.GetRef(PropertyName);
                    xValue = xValue?.ToString();
                    yValue = yValue?.ToString();
                }
                else
                {
                    xValue = buffered ? x.GetValue(property) : x[PropertyName];
                    yValue = buffered ? y.GetValue(property) : y[PropertyName];
                }
                if (property.Format == "SL")
                {
                    int len = xValue.ToString().Length.CompareTo(yValue.ToString().Length);
                    if (len != 0)
                        return Direction == ListSortDirection.Descending ? -len : len;
                }
                rez = ListHelper.Compare(xValue, yValue, null, false);
                if (rez == 0 && Hash)
                    rez = x.CompareTo(y);
            }
            return Direction == ListSortDirection.Descending ? -rez : rez;
        }

        public bool Equals(T xWord, T yWord)
        {
            return xWord.Equals(yWord);
        }

        public int GetHashCode(object obj)
        {
            return obj.GetHashCode();
        }

        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }

        #endregion

        public override bool Equals(object obj)
        {
            if (obj is DBComparer<T>)
                return Direction == ((DBComparer<T>)obj).Direction
                                                     && Table == ((DBComparer<T>)obj).Table
                                                     && PropertyName.Equals(((DBComparer<T>)obj).PropertyName, StringComparison.Ordinal);
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #region IComparer Members

        public int Compare(object x, object y)
        {
            return Compare((T)x, (T)y);
        }

        #endregion
    }
}
