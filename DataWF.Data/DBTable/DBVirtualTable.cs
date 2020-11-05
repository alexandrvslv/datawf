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
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

[assembly: Invoker(typeof(DBVirtualTable<>), nameof(DBVirtualTable<DBItem>.BaseTableName), typeof(DBVirtualTable<>.BaseTableNameInvoker))]
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

        [Browsable(false), XmlIgnore, JsonIgnore]
        public override bool IsSynchronized
        {
            get { return base.IsSynchronized || (BaseTable?.IsSynchronized ?? false); }
            set
            {
                base.IsSynchronized = value;
            }
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
                return filterQuery ?? (filterQuery = new QQuery(query, BaseTable) { TypeFilter = typeof(T) });
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
                    if (BaseTable != null)
                    {
                        Generate();
                    }
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
                    baseTable = value;
                    BaseTableName = value?.Name;
                    if (value != null)
                    {
                        GroupName = value.GroupName;
                        BlockSize = value.BlockSize;
                    }
                }
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
        public override IDBLogTable LogTable
        {
            get { return BaseTable.LogTable; }
            set { }
        }

        [XmlIgnore, JsonIgnore]
        public override string SqlName
        {
            get { return BaseTableName; }
        }

        [XmlIgnore, JsonIgnore]
        public override DBSchema Schema
        {
            get { return base.Schema; }
            set
            {
                base.Schema = value;
                Generate();
            }
        }

        public void Refresh()
        {
            Clear();
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
                    CheckViews(item, NotifyCollectionChangedAction.Remove);
                    RemoveIndexes(item);
                    return true;
                }
                return false;
            }
        }

        public override void Accept(DBItem item)
        {
            if (item is T tItem && tItem.GetType() == typeof(T))
            {
                base.Accept(tItem);
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
                CheckViews(item, NotifyCollectionChangedAction.Add);
            }
        }

        public void OnTableChanged(DBItem item, NotifyCollectionChangedAction type)
        {
            if (item is T view && view.GetType() == typeof(T))
            {
                switch (type)
                {
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
            else if (type == NotifyCollectionChangedAction.Reset)
            {
                Refresh();
            }
        }

        public override void OnItemChanging<V>(DBItem item, string property, DBColumn column, V value)
        {
            if (item is T && item.GetType() == typeof(T))
            {
                base.OnItemChanging<V>(item, property, column == null ? null : Columns[column.Name], value);
            }
        }

        public override void OnItemChanging(DBItem item, string property, DBColumn column, object value)
        {
            if (item is T && item.GetType() == typeof(T))
            {
                base.OnItemChanging(item, property, column == null ? null : Columns[column.Name], value);
            }
        }

        public override void OnItemChanged<V>(DBItem item, string property, DBColumn column, V value)
        {
            if (item is T tItem && tItem.GetType() == typeof(T))
            {
                if (FilterQuery.Parameters.Count != 0 && (FilterQuery.Contains(column?.Name) && !BaseTable.CheckItem(tItem, FilterQuery)))
                {
                    if (items.Remove(tItem))
                    {
                        CheckViews(item, NotifyCollectionChangedAction.Remove);
                        RemoveIndexes(tItem);
                    }
                }
                else
                {
                    base.OnItemChanged<V>(item, property, column == null ? null : Columns[column.Name], value);
                }
            }
        }

        public override void OnItemChanged(DBItem item, string property, DBColumn column, object value)
        {
            if (item is T tItem && tItem.GetType() == typeof(T))
            {
                if (FilterQuery.Parameters.Count != 0 && (FilterQuery.Contains(column?.Name) && !BaseTable.CheckItem(tItem, FilterQuery)))
                {
                    if (items.Remove(tItem))
                    {
                        CheckViews(item, NotifyCollectionChangedAction.Remove);
                        RemoveIndexes(tItem);
                    }
                }
                else
                {
                    base.OnItemChanged(item, property, column == null ? null : Columns[column.Name], value);
                }
            }
        }

        public override DBColumn CheckColumn(string name, Type type, ref bool newCol)
        {
            var column = base.CheckColumn(name, type, ref newCol);
            return column is DBVirtualColumn virtualColumn ? virtualColumn.BaseColumn : column;
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

        public override Task<bool> SaveItem(DBItem row, DBTransaction transaction)
        {
            return BaseTable.SaveItem(row, transaction);
        }

        public override void Clear()
        {
            items.Clear();
            ClearColumnsData(false);
            CheckViews(null, NotifyCollectionChangedAction.Reset);
        }

        public override string FormatSql(DDLType ddlType, bool dependency = false)
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

        public override IEnumerable<DBForeignKey> GetChildRelations()
        {
            foreach (var item in base.GetChildRelations())
                yield return item;
            foreach (var item in BaseTable.GetChildRelations())
                yield return item;
        }

        public override DBColumn ParseColumn(string name)
        {
            return base.ParseColumn(name);
        }

        public void Generate()
        {
            if (BaseTable == null || Columns.Count > 0)
                return;
            var type = typeof(T);
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
                if (existColumn == null || reference.Reference == null)
                    continue;
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

        public class BaseTableNameInvoker : Invoker<DBVirtualTable<T>, string>
        {
            public static readonly BaseTableNameInvoker Instance = new BaseTableNameInvoker();
            public override string Name => nameof(DBVirtualTable<T>.BaseTableName);

            public override bool CanWrite => true;

            public override string GetValue(DBVirtualTable<T> target) => target.BaseTableName;

            public override void SetValue(DBVirtualTable<T> target, string value) => target.BaseTableName = value;
        }
    }


}
