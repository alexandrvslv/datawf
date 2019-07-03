/*
 DBColumnList.cs
 
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
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using DataWF.Common;
using System.Linq;
using System.Collections.Specialized;

namespace DataWF.Data
{
    public class DBColumnList<T> : DBTableItemList<T> where T : DBColumn, new()
    {
        public static readonly Invoker<T, string> GroupNameInvoker = new Invoker<T, string>(nameof(DBColumn.GroupName), p => p.GroupName);
        public static readonly Invoker<T, string> PropertyInvoker = new Invoker<T, string>(nameof(DBColumn.Property), p => p.Property);
        public static readonly Invoker<T, bool> IsViewInvoker = new Invoker<T, bool>(nameof(DBColumn.IsView), p => p.IsView);
        public static readonly Invoker<T, bool> IsReferenceInvoker = new Invoker<T, bool>(nameof(DBColumn.IsReference), p => p.IsReference);
        public static readonly Invoker<T, string> ReferenceTableInvoker = new Invoker<T, string>(nameof(DBColumn.ReferenceTable), p => p.ReferenceTable?.Name);

        public DBColumnList(DBTable table)
            : base(table)
        {
            Indexes.Add(GroupNameInvoker);
            Indexes.Add(PropertyInvoker);
            Indexes.Add(IsViewInvoker);
            Indexes.Add(IsReferenceInvoker);
            Indexes.Add(ReferenceTableInvoker);
        }

        protected override void OnPropertyChanged(string property)
        {
            base.OnPropertyChanged(property);
            if (Table != null && Table.Schema != null)
            {
                Table.ClearCache();
            }
        }

        public override void RemoveInternal(T item, int index)
        {
            base.RemoveInternal(item, index);
            if (item.Index != null)
            {
                item.Clear();
                item.Index.Dispose();
                item.Index = null;
            }
        }

        public new void Clear()
        {
            base.Clear();
        }

        public override void InsertInternal(int index, T item)
        {
            if (Contains(item))
            {
                return;
            }
            if (Contains(item.Name))
            {
                throw new InvalidOperationException($"Columns name duplication {item.Name}");
            }
            // if (col.Order == -1 || col.Order > this.Count)
            if (item.IsPrimaryKey && index > 1)
                index = 1;
            if (item.IsTypeKey)
                index = 0;
            item.Order = index;

            base.InsertInternal(index, item);

            if (item.IsPrimaryKey)
            {
                DBConstraint primary = null;
                foreach (var constraint in Table.Constraints.GetByColumn(Table.PrimaryKey))
                {
                    if (constraint.Type == DBConstraintType.Primary)
                    {
                        primary = constraint;
                        break;
                    }
                }
                if (primary == null && Table.PrimaryKey != null)
                {
                    primary = new DBConstraint() { Column = Table.PrimaryKey, Type = DBConstraintType.Primary };
                    primary.GenerateName();
                    Table.Constraints.Add(primary);
                    //Table.DefaultComparer = item.CreateComparer(); Commented for poerformance of Index creation
                }
            }
            item.CheckPull();
        }

        public DBColumn Add(string name)
        {
            return Add(name, typeof(string), 0);
        }

        public DBColumn Add(string name, Type t)
        {
            return Add(name, t, 0);
        }

        public DBColumn Add(string name, DBTable reference)
        {
            DBColumn column = Add(name, reference.PrimaryKey.DataType, reference.PrimaryKey.Size);
            column.ReferenceTable = reference;
            return column;
        }

        public DBColumn Add(string name, Type t, int size)
        {
            if (Contains(name))
                return this[name];
            var column = new DBColumn(name, t, size) { Table = Table };
            Add((T)column);
            return column;
        }

        public IEnumerable<DBColumn> GetByGroup(DBColumnGroup group)
        {
            return GetByGroup(group.Name);
        }

        public IEnumerable<DBColumn> GetByGroup(string groupName)
        {
            return string.IsNullOrEmpty(groupName) ? null : Select(GroupNameInvoker, CompareType.Equal, groupName);
        }

        public IEnumerable<DBColumn> GetByReference(DBTable table)
        {
            return Select(ReferenceTableInvoker, CompareType.Equal, table.Name);
        }

        public IEnumerable<DBColumn> GetIsReference()
        {
            return Select(IsReferenceInvoker, CompareType.Equal, true);
        }

        public IEnumerable<DBColumn> GetIsView()
        {
            return Select(IsViewInvoker, CompareType.Equal, true);
        }

        public DBColumn GetByProperty(string property)
        {
            var columns = Select(PropertyInvoker, CompareType.Equal, property);
            if (columns.Count() > 1)
            {
                return columns.Where(p => p.Culture == Locale.Instance.Culture).FirstOrDefault();
            }
            return columns.FirstOrDefault();
        }

        public DBColumn GetByKey(DBColumnKeys key)
        {
            foreach (DBColumn column in this)
            {
                if ((column.Keys & key) == key)
                {
                    return column;
                }
            }
            return null;
        }
    }
}
