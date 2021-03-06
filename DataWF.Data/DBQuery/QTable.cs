﻿//  The MIT License (MIT)
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
using System.Data;

namespace DataWF.Data
{
    public class QTable : QItem
    {
        protected JoinType join;
        protected DBTable table;
        protected string tableName = null;

        public QTable()
        { }

        public QTable(DBTable table, string alias = null)
        {
            Table = table;
            this.alias = alias;
        }


        public string TableName
        {
            get { return tableName; }
            set
            {
                if (tableName != value)
                {
                    tableName = value;
                    table = null;
                    OnPropertyChanged(nameof(TableName));
                }
            }
        }

        public JoinType Join
        {
            get { return join; }
            set
            {
                if (join != value)
                {
                    join = value;
                    OnPropertyChanged(nameof(Join));
                }
            }
        }

        public DBTable BaseTable
        {
            get => Table is IDBVirtualTable virtualTable ? virtualTable.BaseTable : Table;
        }

        public override DBTable Table
        {
            get
            {
                if (table == null)
                    table = DBService.Schems.ParseTable(tableName);
                return table;
            }
            set
            {
                if (Table != value)
                {
                    TableName = value?.FullName;
                    Text = value?.Name;
                    table = value;
                    OnPropertyChanged(nameof(Table));
                }
            }
        }

        public override string Format(IDbCommand command = null)
        {
            var schema = Table.Schema.Connection.Schema;

            return $"{Join.Format()} {Table?.FormatQTable(alias) ?? ($"{text} {alias}")}";
        }
    }
}
