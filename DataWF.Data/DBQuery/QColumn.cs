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
using System.Data;
using System.Linq;

namespace DataWF.Data
{
    public sealed class QColumn : QItem
    {
        private DBColumn column;
        private object value;

        public QColumn()
        {
            IsReference = true;
        }

        public QColumn(DBColumn column) : this()
        {
            Column = column;
        }

        public override string Name
        {
            get => Column?.Name;
            set { }
        }

        public override IDBTable Table
        {
            get => Column?.Table;
            set { }
        }

        public override bool IsReference
        {
            get => base.IsReference && Value == null;
            set => base.IsReference = value;
        }

        public DBColumn Column
        {
            get => column;
            set
            {
                if (Column != value)
                {
                    Name = value?.Name;
                    column = value;
                }
            }
        }

        public string FullName
        {
            get => $"{Table}.{Column}";
        }

        public object Value { get => value; set => this.value = value; }

        public override void Dispose()
        {
            column = null;
            base.Dispose();
        }

        public override string Format(IDbCommand command = null)
        {
            if (Column == null)
                return Name;
            else if (command != null
                && (Column.ColumnType == DBColumnTypes.Internal
                || Column.ColumnType == DBColumnTypes.Expression
                || Column.ColumnType == DBColumnTypes.Code))
                return string.Empty;
            else if (Column.ColumnType == DBColumnTypes.Query && Column.Table.Type != DBTableType.View)
                return $"({Column.Query}) as {Name}";
            else
                return $"{(TableAlias != null ? (TableAlias + ".") : "")}{Name}";
        }

        public override object GetValue(DBItem item)
        {
            return value ?? (item == null ? null : Column.GetValue(item));
        }

        public override bool CheckItem(DBItem item, object val2, CompareType comparer)
        {
            return Column.CheckItem(item, val2, comparer);
        }

        public override string ToString()
        {
            return Column == null ? base.ToString() : Column.ToString();
        }

        public IComparer CreateComparer(Type type, ListSortDirection direction = ListSortDirection.Ascending)
        {
            return Column?.CreateComparer(type, direction);
        }

        public IComparer<T> CreateComparer<T>(ListSortDirection direction = ListSortDirection.Ascending)
        {
            return (IComparer<T>)Column?.CreateComparer(typeof(T), direction);
        }
    }
}