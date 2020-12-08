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
    public class QColumn : QItem, IInvokerExtension
    {
        protected DBColumn columnn;
        private object temp;
        protected string columnName;
        private string prefix;

        public QColumn()
        {
        }

        public QColumn(string name)
            : base(name)
        {
            columnName = name;
        }

        public QColumn(DBColumn column)
            : base(column.Name)
        {
            Column = column;
        }

        public string ColumnName
        {
            get => columnName;
            set
            {
                if (columnName != value)
                {
                    columnName = value;
                    columnn = null;
                }
                OnPropertyChanged(nameof(ColumnName));
            }
        }

        public virtual DBColumn Column
        {
            get => columnn ?? (columnName != null ? (base.Table?.ParseColumn(columnName) ?? DBService.Schems.ParseColumn(columnName)) : null);
            set
            {
                if (Column != value)
                {
                    ColumnName = value?.FullName;
                    base.Text = value?.Name;
                    //prefix = value.Table.Code;
                    columnn = value;
                }
            }
        }

        public DBTable BaseTable
        {
            get => Table is IDBVirtualTable virtualTable ? virtualTable.BaseTable : Table;
        }

        public override DBTable Table
        {
            get => Column?.Table ?? base.Table;
            set { }
        }

        public QTable QTable
        {
            get => Query.Tables.FirstOrDefault(p => p.BaseTable == BaseTable);
        }

        public string FullName
        {
            get => $"{Table}.{Column}";
        }

        public string Prefix
        {
            get => prefix ?? QTable?.Alias;
            set
            {
                if (prefix != value)
                {
                    prefix = value;
                    OnPropertyChanged(nameof(Prefix));
                }
            }
        }

        public object Temp { get => temp; set => temp = value; }

        public override void Dispose()
        {
            columnn = null;
            base.Dispose();
        }

        public override string Format(IDbCommand command = null)
        {
            if (Column == null)
                return text;
            else if (command != null
                && (Column.ColumnType == DBColumnTypes.Internal
                || Column.ColumnType == DBColumnTypes.Expression
                || Column.ColumnType == DBColumnTypes.Code))
                return string.Empty;
            else if (Column.ColumnType == DBColumnTypes.Query && Column.Table.Type != DBTableType.View)
                return $"({Column.Query}) as {text}";
            else
                return $"{(Prefix != null ? (Prefix + ".") : "")}{text}{(alias != null ? (" as " + alias) : "")}";
        }

        public override object GetValue(DBItem row)
        {
            return temp ?? Column.GetValue(row);
        }

        public override string ToString()
        {
            return Column == null ? base.ToString() : Column.ToString();
        }

        public IListIndex CreateIndex(bool concurrent)
        {
            throw new NotImplementedException();
        }

        public IListIndex CreateIndex<T>(bool concurrent)
        {
            throw new NotImplementedException();
        }

        public IQueryParameter CreateParameter(Type type)
        {
            throw new NotImplementedException();
        }

        public IQueryParameter<TT> CreateParameter<TT>()
        {
            throw new NotImplementedException();
        }

        public IQueryParameter CreateParameter(Type type, CompareType comparer, object value)
        {
            throw new NotImplementedException();
        }

        public IQueryParameter CreateParameter(Type type, LogicType logic, CompareType comparer, object value = null, QueryGroup group = QueryGroup.None)
        {
            throw new NotImplementedException();
        }

        public IQueryParameter<TT> CreateParameter<TT>(CompareType comparer, object value)
        {
            throw new NotImplementedException();
        }

        public IQueryParameter<TT> CreateParameter<TT>(LogicType logic, CompareType comparer, object value = null, QueryGroup group = QueryGroup.None)
        {
            throw new NotImplementedException();
        }

        public IComparer CreateComparer(Type type, ListSortDirection direction = ListSortDirection.Ascending)
        {
            return Column?.CreateComparer(type, direction);
        }

        public IComparer<TT> CreateComparer<TT>(ListSortDirection direction = ListSortDirection.Ascending)
        {
            return (IComparer<TT>)Column?.CreateComparer(typeof(TT), direction);
        }

       
    }
}