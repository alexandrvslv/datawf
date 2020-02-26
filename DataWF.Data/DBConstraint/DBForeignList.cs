﻿using DataWF.Common;
/*
DBConstraintList.cs

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
using System.Collections.Generic;

namespace DataWF.Data
{
    public class DBForeignList : DBConstraintList<DBForeignKey>
    {

        public DBForeignList(DBTable table) : base(table)
        {
            Indexes.Add(DBForeignKey.ReferenceNameInvoker.Instance);
            Indexes.Add(DBForeignKey.ReferenceTableNameInvoker.Instance);
            Indexes.Add(DBForeignKey.PropertyInvoker.Instance);
        }

        public DBForeignKey GetForeignByColumn(DBColumn column)
        {
            return SelectOne(nameof(DBForeignKey.ColumnName), CompareType.Equal, column.FullName);
        }

        public DBForeignKey GetByProperty(string property)
        {
            return SelectOne(nameof(DBForeignKey.Property), CompareType.Equal, property);
        }

        public IEnumerable<DBForeignKey> GetByReference(DBColumn reference)
        {
            return Select(DBForeignKey.ReferenceNameInvoker.Instance, CompareType.Equal, reference.FullName);
        }

        public IEnumerable<DBForeignKey> GetByReference(DBTable reference)
        {
            return Select(DBForeignKey.ReferenceTableNameInvoker.Instance, CompareType.Equal, reference.FullName);
        }

        public DBForeignKey GetByColumns(DBColumn column, DBColumn reference)
        {
            foreach (var item in items)
            {
                if (item.Column == column && item.Reference == reference)
                {
                    return item;
                }
            }
            return null;
        }

        public void CacheChildRelations()
        {
            foreach (var item in this)
            {
                var table = item.ReferenceTable;
                if (table != null)
                {
                    table.ChildRelations.Add(item);
                }
                if (table is IDBVirtualTable virtualTable)
                {
                    virtualTable.BaseTable.ChildRelations.Add(item);
                }
            }
        }
    }


}
