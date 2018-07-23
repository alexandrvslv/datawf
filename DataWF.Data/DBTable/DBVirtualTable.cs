/*
 DBTable.cs
 
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
using System.ComponentModel;
using DataWF.Common;
using System.Xml.Serialization;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Linq;

namespace DataWF.Data
{
    public class DBVirtualTable<T> : DBTable<T>, IDBVirtualTable where T : DBItem, new()
    {
        private DBTable baseTable;
        protected string baseTableName;
        private QQuery filterQuery;

        public DBVirtualTable()
        {
            //Columns.Indexes.Add(baseNameInvoker);
        }


        [JsonIgnore, XmlIgnore]
        public override DBColumnList<DBColumn> Columns
        {
            get { return base.Columns; }
            set { base.Columns = value; }
        }

        [JsonIgnore, XmlIgnore]
        public override DBColumnGroupList ColumnGroups
        {
            get { return BaseTable.ColumnGroups; }
            set { value?.Dispose(); }
        }

        [JsonIgnore, XmlIgnore]
        public override DBConstraintList<DBConstraint> Constraints
        {
            get { return base.Constraints; }
            set { base.Constraints = value; }
        }

        [JsonIgnore, XmlIgnore]
        public override DBIndexList Indexes
        {
            get { return BaseTable.Indexes; }
            set { value?.Dispose(); }
        }

        [JsonIgnore, XmlIgnore]
        public override DBForeignList Foreigns
        {
            get { return base.Foreigns; }
            set { base.Foreigns = value; }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public QQuery FilterQuery
        {
            get
            {
                if (BaseTable == null)
                    return null;
                if (filterQuery == null)
                {
                    filterQuery = new QQuery(query, BaseTable);
                    filterQuery.TypeFilter = typeof(T);
                }
                return filterQuery;
            }
        }

        [XmlAttribute, Browsable(false), Category("Database")]
        public string BaseTableName
        {
            get { return baseTableName; }
            set
            {
                if (baseTableName != value)
                {
                    baseTableName = value;
                    baseTable = null;
                    filterQuery = null;
                    OnPropertyChanged(nameof(BaseTableName), DDLType.Alter);
                }
            }
        }

        [XmlIgnore, JsonIgnore, Category("Database")]
        public DBTable BaseTable
        {
            get { return baseTable ?? (baseTable = Schema?.Tables[baseTableName]); }
            set
            {
                if (BaseTable != value)
                {
                    Columns.Clear();

                    BaseTableName = value?.Name;
                    baseTable = value;
                    if (value != null)
                    {
                        GroupName = value.GroupName;
                        BlockSize = value.BlockSize;
                    }
                }
                Generate();
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override int BlockSize { get => BaseTable.BlockSize; set => base.BlockSize = value; }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override string SequenceName { get => BaseTable.SequenceName; set => base.SequenceName = value; }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override bool IsLoging
        {
            get { return BaseTable.IsLoging; }
            set { BaseTable.IsLoging = value; }
        }

        [XmlIgnore, JsonIgnore]
        public override DBLogTable LogTable
        {
            get { return BaseTable.LogTable; }
            set { }
        }

        public void CheckItem(DBItem item, string property, NotifyCollectionChangedAction type)
        {
            if (item is T view)
            {
                switch (type)
                {
                    case NotifyCollectionChangedAction.Reset:
                        if (FilterQuery.Parameters.Count != 0 && (FilterQuery.Contains(property) && !BaseTable.CheckItem(item, FilterQuery)))
                        {
                            Remove(view);
                        }
                        else
                        {
                            OnItemChanged(view, property, type);
                        }

                        break;
                    case NotifyCollectionChangedAction.Add:
                        if (BaseTable.CheckItem(item, FilterQuery))
                        {
                            Add(view);
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        Remove(view);
                        break;
                }
            }
            else if (item == null && type == NotifyCollectionChangedAction.Reset)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            items.Clear();
            foreach (T item in BaseTable.SelectItems(FilterQuery))
            {
                Add(item);
            }
        }

        public override bool Remove(T item)
        {
            if (item.Attached)
            {
                return BaseTable.Remove(item);
            }
            else
            {
                if (items.Remove(item))
                {
                    OnItemChanged(item, null, NotifyCollectionChangedAction.Remove);
                    RemoveIndexes(item);
                    return true;
                }
                return false;
            }
        }

        public override void Add(T item)
        {
            if (!item.Attached)
            {
                BaseTable.Add(item);
            }
            else
            {
                items.Add(item);
                AddIndexes(item);
                OnItemChanged(item, null, NotifyCollectionChangedAction.Add);
            }
        }

        public override void AddIndex(DBItem item, DBColumn column, object value)
        {
            var virtualColumn = column is DBVirtualColumn ? column : Columns[column.Name];
            BaseTable.AddIndex(item, ((DBVirtualColumn)virtualColumn).BaseColumn, value);
            base.AddIndex(item, virtualColumn, value);
        }

        public override void RemoveIndex(DBItem item, DBColumn column, object value)
        {
            var virtualColumn = column is DBVirtualColumn ? column : Columns[column.Name];
            BaseTable.RemoveIndex(item, ((DBVirtualColumn)virtualColumn).BaseColumn, value);
            base.RemoveIndex(item, virtualColumn, value);
        }

        public override string SqlName
        {
            get { return BaseTableName; }
        }

        public override DBItem NewItem(DBUpdateState state = DBUpdateState.Insert, bool def = true, int typeIndex = 0)
        {
            if (typeIndex == 0)
                typeIndex = GetTypeIndex(typeof(T));
            return BaseTable.NewItem(state, def, typeIndex);
        }

        public override DBItemType GetItemType(int typeIndex)
        {
            return BaseTable.GetItemType(typeIndex);
        }

        public override int GetTypeIndex(Type type)
        {
            return BaseTable.GetTypeIndex(type);
        }

        public override int NextHash()
        {
            return BaseTable.NextHash();
        }

        //public override bool SaveItem(DBItem row)
        //{
        //    return BaseTable.SaveItem(row);
        //}

        public override void Clear()
        {
            BaseTable.Clear();
            base.Clear();
        }

        public override string FormatSql(DDLType ddlType)
        {
            var ddl = new StringBuilder();
            Schema.System.Format(ddl, (IDBVirtualTable)this, ddlType);
            return ddl.ToString();
        }

        public override void Dispose()
        {
            BaseTable?.RemoveVirtual(this);
            filterQuery?.Dispose();
            base.Dispose();
        }

        public void Generate()
        {
            if (BaseTable == null)
                return;
            foreach (DBColumn column in BaseTable.Columns)
            {
                var exist = (DBVirtualColumn)ParseColumn(column.Name);
                if (exist == null)
                {
                    exist = new DBVirtualColumn(column);
                    Columns.Add(exist);
                    exist.DisplayName = column.DisplayName;
                }
                else
                {
                    exist.BaseColumn = column;
                }
            }

            foreach (DBForeignKey reference in BaseTable.Foreigns)
            {
                var existColumn = ParseColumn(reference.Column.Name);

                var exist = Foreigns.GetByColumns(existColumn, reference.Reference);
                if (exist == null)
                {
                    exist = new DBForeignKey()
                    {
                        Column = existColumn,
                        Reference = reference.Reference,
                        Property = reference.Property
                    };
                    exist.GenerateName();
                    Foreigns.Add(exist);
                }
            }

            foreach (DBConstraint constraint in BaseTable.Constraints)
            {
                var existColumn = ParseColumn(constraint.Column.Name);
                var exist = Constraints.GetByColumnAndTYpe(existColumn, constraint.Type).FirstOrDefault();
                if (exist == null)
                {
                    exist = new DBConstraint
                    {
                        Column = existColumn,
                        Type = constraint.Type,
                        Value = constraint.Value
                    };
                    exist.GenerateName();
                    Constraints.Add(exist);
                }
            }
        }
    }
}
