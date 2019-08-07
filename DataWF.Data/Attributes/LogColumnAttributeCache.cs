/*
 ColumnConfig.cs
 
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
using System.Globalization;
using System.Reflection;

namespace DataWF.Data
{
    public class LogColumnAttributeCache : ColumnAttributeCache
    {
        public LogColumnAttributeCache(LogTableAttributeCache table, PropertyInfo property, LogColumnAttribute columnAttribute, CultureInfo culture)
            : base(table, property, columnAttribute, culture)
        {
        }
        public LogColumnAttributeCache(LogTableAttributeCache table, PropertyInfo property, LogColumnAttribute columnAttribute)
            : base(table, property, columnAttribute)
        {
        }

        public LogColumnAttribute LogAttribute => base.Attribute as LogColumnAttribute;

        public LogTableAttributeCache LogTable => Table as LogTableAttributeCache;

        public DBLogColumn LogColumn
        {
            get => Column as DBLogColumn;
            set => Column = value;
        }

        public ColumnAttributeCache BaseColumn => LogTable.BaseTableAttribute.GetColumn(LogAttribute.BaseName);

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
            Column.ReferencePropertyInfo = ReferenceProperty;
            Column.DefaultValues = DefaultValues;
        }
    }
}
