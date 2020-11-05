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
using System.Globalization;
using System.Reflection;

namespace DataWF.Data
{
    public class LogColumnGenerator : ColumnGenerator
    {
        public LogColumnGenerator(LogTableGenerator table, PropertyInfo property, LogColumnAttribute columnAttribute, CultureInfo culture)
            : base(table, property, columnAttribute, culture)
        {
        }
        public LogColumnGenerator(LogTableGenerator table, PropertyInfo property, LogColumnAttribute columnAttribute)
            : base(table, property, columnAttribute)
        {
        }

        public LogColumnAttribute LogAttribute => base.Attribute as LogColumnAttribute;

        public LogTableGenerator LogTable => Table as LogTableGenerator;

        public DBLogColumn LogColumn
        {
            get => Column as DBLogColumn;
            set => Column = value;
        }

        public override int Size => BaseColumn?.Size ?? base.Size;

        public ColumnGenerator BaseColumn => LogTable?.BaseTableGenerator?.GetColumn(LogAttribute?.BaseName);

        public override DBColumn CreateColumn(string name)
        {
            return new DBLogColumn() { Name = name, Table = Table.Table };
        }

        public override void GenerateColumn(DBTable table)
        {
            if (!(table is IDBLogTable))
                throw new Exception("Log Column Expect Log Table");
            if (BaseColumn?.Column == null)
                throw new Exception("Log Column Expect Base Column");

            LogColumn = table.Columns[ColumnName] as DBLogColumn;
            if (LogColumn == null)
            {
                LogColumn = (DBLogColumn)CreateColumn(ColumnName);
            }
            LogColumn.BaseColumn = BaseColumn.Column;
            Column.Property = PropertyName;
            Column.PropertyInfo = PropertyInfo;
            Column.ReferencePropertyInfo = ReferencePropertyInfo;
            Column.DefaultValues = DefaultValues;
            if (!table.Columns.Contains(Column.Name))
            {
                table.Columns.Add(Column);
            }
        }
    }
}
