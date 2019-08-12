using DataWF.Common;
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
            Indexes.Add(DBForeignKeyReferenceNameInvoker.Instance);
            Indexes.Add(DBForeignKeyReferenceTableNameInvoker.Instance);
            Indexes.Add(DBForeignKeyPropertyInvoker.Instance);
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
            return Select(DBForeignKeyReferenceNameInvoker.Instance, CompareType.Equal, reference.FullName);
        }

        public IEnumerable<DBForeignKey> GetByReference(DBTable reference)
        {
            return Select(DBForeignKeyReferenceTableNameInvoker.Instance, CompareType.Equal, reference.FullName);
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

    [Invoker(typeof(DBForeignKey), nameof(DBForeignKey.ReferenceName))]
    public class DBForeignKeyReferenceNameInvoker : Invoker<DBForeignKey, string>
    {
        public static readonly DBForeignKeyReferenceNameInvoker Instance = new DBForeignKeyReferenceNameInvoker();
        public DBForeignKeyReferenceNameInvoker()
        {
            Name = nameof(DBForeignKey.ReferenceName);
        }

        public override bool CanWrite => true;

        public override string GetValue(DBForeignKey target) => target.ReferenceName;

        public override void SetValue(DBForeignKey target, string value) => target.ReferenceName = value;
    }

    [Invoker(typeof(DBForeignKey), nameof(DBForeignKey.Property))]
    public class DBForeignKeyPropertyInvoker : Invoker<DBForeignKey, string>
    {
        public static readonly DBForeignKeyPropertyInvoker Instance = new DBForeignKeyPropertyInvoker();

        public override bool CanWrite => throw new System.NotImplementedException();

        public DBForeignKeyPropertyInvoker()
        {
            Name = nameof(DBForeignKey.Property);
        }

        public override string GetValue(DBForeignKey target) => target.Property;

        public override void SetValue(DBForeignKey target, string value) => target.Property = value;
    }

    [Invoker(typeof(DBForeignKey), nameof(DBForeignKey.ReferenceTableName))]
    public class DBForeignKeyReferenceTableNameInvoker : Invoker<DBForeignKey, string>
    {
        public static readonly DBForeignKeyReferenceTableNameInvoker Instance = new DBForeignKeyReferenceTableNameInvoker();

        public DBForeignKeyReferenceTableNameInvoker()
        {
            Name = nameof(DBForeignKey.ReferenceTableName);
        }

        public override bool CanWrite => false;

        public override string GetValue(DBForeignKey target) => target.ReferenceTableName;

        public override void SetValue(DBForeignKey target, string value) { }
    }
}
