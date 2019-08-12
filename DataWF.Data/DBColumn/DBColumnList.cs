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
        public DBColumnList(DBTable table)
            : base(table)
        {
            Indexes.Add(DBColumnGroupNameInvoker<T>.Instance);
            Indexes.Add(DBColumnPropertyInvoker<T>.Instance);
            Indexes.Add(DBColumnReferencePropertyInvoker<T>.Instance);
            Indexes.Add(DBColumnIsViewInvoker<T>.Instance);
            Indexes.Add(DBColumnIsReferenceInvoker<T>.Instance);
            Indexes.Add(DBColumnReferenceTableInvoker<T>.Instance);
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
            return string.IsNullOrEmpty(groupName) ? null : Select(DBColumnGroupNameInvoker<T>.Instance, CompareType.Equal, groupName);
        }

        public IEnumerable<DBColumn> GetByReference(DBTable table)
        {
            return Select(DBColumnReferenceTableInvoker<T>.Instance, CompareType.Equal, table.Name);
        }

        public IEnumerable<DBColumn> GetIsReference()
        {
            return Select(DBColumnIsReferenceInvoker<T>.Instance, CompareType.Equal, true);
        }

        public IEnumerable<DBColumn> GetIsView()
        {
            return Select(DBColumnIsViewInvoker<T>.Instance, CompareType.Equal, true);
        }

        public DBColumn GetByProperty(string property)
        {
            var columns = Select(DBColumnPropertyInvoker<T>.Instance, CompareType.Equal, property);
            if (columns.Count() > 1)
            {
                return columns.Where(p => p.Culture == Locale.Instance.Culture).FirstOrDefault();
            }
            return columns.FirstOrDefault();
        }

        public DBColumn GetByReferenceProperty(string property)
        {
            var columns = Select(DBColumnReferencePropertyInvoker<T>.Instance, CompareType.Equal, property);
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

    [Invoker(typeof(DBColumn), nameof(DBColumn.GroupName))]
    public class DBColumnGroupNameInvoker<T> : Invoker<T, string> where T : DBColumn
    {
        public static readonly DBColumnGroupNameInvoker<T> Instance = new DBColumnGroupNameInvoker<T>();

        public DBColumnGroupNameInvoker()
        {
            Name = nameof(DBColumn.GroupName);
        }

        public override bool CanWrite => true;

        public override string GetValue(T target) => target.GroupName;

        public override void SetValue(T target, string value) => target.GroupName = value;
    }

    [Invoker(typeof(DBColumn), nameof(DBColumn.Property))]
    public class DBColumnPropertyInvoker<T> : Invoker<T, string> where T : DBColumn
    {
        public static readonly DBColumnPropertyInvoker<T> Instance = new DBColumnPropertyInvoker<T>();

        public DBColumnPropertyInvoker()
        {
            Name = nameof(DBColumn.Property);
        }

        public override bool CanWrite => true;

        public override string GetValue(T target) => target.Property;

        public override void SetValue(T target, string value) => target.Property = value;
    }

    [Invoker(typeof(DBColumn), nameof(DBColumn.ReferenceProperty))]
    public class DBColumnReferencePropertyInvoker<T> : Invoker<T, string> where T : DBColumn
    {
        public static readonly DBColumnReferencePropertyInvoker<T> Instance = new DBColumnReferencePropertyInvoker<T>();

        public DBColumnReferencePropertyInvoker()
        {
            Name = nameof(DBColumn.ReferenceProperty);
        }

        public override bool CanWrite => true;

        public override string GetValue(T target) => target.ReferenceProperty;

        public override void SetValue(T target, string value) => target.ReferenceProperty = value;
    }

    [Invoker(typeof(DBColumn), nameof(DBColumn.ReferenceTable))]
    public class DBColumnReferenceTableInvoker<T> : Invoker<T, string> where T : DBColumn
    {
        public static readonly DBColumnReferenceTableInvoker<T> Instance = new DBColumnReferenceTableInvoker<T>();

        public DBColumnReferenceTableInvoker()
        {
            Name = nameof(DBColumn.ReferenceTable);
        }

        public override bool CanWrite => false;

        public override string GetValue(T target) => target.ReferenceTable?.Name;

        public override void SetValue(T target, string value) { }
    }

    [Invoker(typeof(DBColumn), nameof(DBColumn.IsView))]
    public class DBColumnIsViewInvoker<T> : Invoker<T, bool> where T : DBColumn
    {
        public static readonly DBColumnIsViewInvoker<T> Instance = new DBColumnIsViewInvoker<T>();

        public DBColumnIsViewInvoker()
        {
            Name = nameof(DBColumn.IsView);
        }

        public override bool CanWrite => false;

        public override bool GetValue(T target) => target.IsView;

        public override void SetValue(T target, bool value) { }
    }

    [Invoker(typeof(DBColumn), nameof(DBColumn.IsReference))]
    public class DBColumnIsReferenceInvoker<T> : Invoker<T, bool> where T : DBColumn
    {
        public static readonly DBColumnIsReferenceInvoker<T> Instance = new DBColumnIsReferenceInvoker<T>();

        public DBColumnIsReferenceInvoker()
        {
            Name = nameof(DBColumn.IsReference);
        }

        public override bool CanWrite => false;

        public override bool GetValue(T target) => target.IsReference;

        public override void SetValue(T target, bool value) { }
    }
}
