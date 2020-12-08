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
using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace DataWF.Data
{
    //public class DBComparer : DBComparer<DBItem, object>
    //{
    //    public DBComparer(DBColumn column, ListSortDirection direction = ListSortDirection.Ascending)
    //        : base(column, direction)
    //    { }

    //    public DBComparer(string table, string column, ListSortDirection direction = ListSortDirection.Ascending)
    //        : base(table, column, direction)
    //    { }

    //    public DBComparer(DBTable table, string column, ListSortDirection direction = ListSortDirection.Ascending)
    //        : base(table, column, direction)
    //    { }
    //}

    public class DBComparer<T, K> : IComparer<T>, IComparer where T : DBItem
    {
        public DBTable Table;
        public string PropertyName;
        public string Format;
        public ListSortDirection Direction;
        public CompareType Comparer;
        public K Value;
        private readonly DBColumn property;
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
            refernce = property != null && property.IsReference && typeof(K) == typeof(string);
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
        public int Compare(T x, K key)
        {
            var xValue = buffered ? x.GetValue<K>(property) : (K)x[PropertyName];
            if (Format != null && xValue is IFormattable xFormat && key is IFormattable keyFormat)
            {
                return string.Compare(xFormat.ToString(Format, CultureInfo.InvariantCulture), keyFormat.ToString(Format, CultureInfo.InvariantCulture), StringComparison.Ordinal);
            }
            return Direction == ListSortDirection.Ascending
                ? ListHelper.Compare<K>(xValue, key)
                : -ListHelper.Compare<K>(xValue, key);
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
            int rez;
            if (x == null)
                rez = (y == null) ? 0 : -1;
            else if (y == null)
                rez = 1;
            else if (x.Equals(y))
                rez = 0;
            else
            {
                K xValue, yValue;
                if (refernce)
                {
                    var xReference = buffered ? x.GetReference(property) : x.GetReference(PropertyName);
                    var yReference = buffered ? y.GetReference(property) : y.GetReference(PropertyName);
                    xValue = (K)(object)xReference?.ToString();
                    yValue = (K)(object)yReference?.ToString();
                }
                else
                {
                    xValue = buffered ? x.GetValue<K>(property) : (K)x[PropertyName];
                    yValue = buffered ? y.GetValue<K>(property) : (K)y[PropertyName];
                }
                if (property.Format == "SL")
                {
                    int len = xValue.ToString().Length.CompareTo(yValue.ToString().Length);
                    if (len != 0)
                        return Direction == ListSortDirection.Descending ? -len : len;
                }
                rez = ListHelper.Compare<K>(xValue, yValue);
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
            if (obj is DBComparer<T, K>)
                return Direction == ((DBComparer<T, K>)obj).Direction
                    && Table == ((DBComparer<T, K>)obj).Table
                    && PropertyName.Equals(((DBComparer<T, K>)obj).PropertyName, StringComparison.Ordinal);
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
