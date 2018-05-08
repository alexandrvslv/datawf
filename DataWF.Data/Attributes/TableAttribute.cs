/*
 BaseConfig.cs
 
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
using DataWF.Data;
using DataWF.Common;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Reflection;
using System.Diagnostics;

namespace DataWF.Data
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TableAttribute : Attribute
    {
        static readonly Invoker<ColumnAttribute, string> columnNameInvoker = new Invoker<ColumnAttribute, string>(nameof(ColumnAttribute.ColumnName), (item) => item.ColumnName);
        static readonly Invoker<ColumnAttribute, string> propertyInvoker = new Invoker<ColumnAttribute, string>(nameof(ColumnAttribute.Property), (item) => item.Property);
        static readonly Invoker<IndexAttribute, string> IndexNameinvoker = new Invoker<IndexAttribute, string>(nameof(IndexAttribute.IndexName), (item) => item.IndexName);

        private DBSchema cacheSchema;
        private DBTable cacheTable;
        private DBTableGroup cacheGroup;
        private SelectableList<ColumnAttribute> cacheColumns = new SelectableList<ColumnAttribute>();
        private SelectableList<ReferenceAttribute> cacheReferences = new SelectableList<ReferenceAttribute>();
        private SelectableList<IndexAttribute> cacheIndexes = new SelectableList<IndexAttribute>();
        private SelectableList<ItemTypeAttribute> cacheItemTypes = new SelectableList<ItemTypeAttribute>();

        public TableAttribute(string name, string groupName)
        {
            cacheColumns.Indexes.Add(columnNameInvoker);
            cacheColumns.Indexes.Add(propertyInvoker);
            cacheIndexes.Indexes.Add(IndexNameinvoker);
            TableName = name;
            GroupName = groupName ?? "Default";
        }

        public string TableName { get; set; }

        public virtual string GroupName { get; set; }

        [DefaultValue(DBTableType.Table)]
        protected DBTableType TableType { get; set; } = DBTableType.Table;

        [XmlIgnore]
        public DBSchema Schema
        {
            get { return cacheSchema; }
            set { cacheSchema = value; }
        }

        [XmlIgnore]
        public DBTable Table
        {
            get { return cacheTable ?? (cacheTable = DBService.ParseTable(TableName)); }
            internal set { cacheTable = value; }
        }

        [XmlIgnore]
        public DBTableGroup TableGroup
        {
            get { return cacheGroup ?? (cacheGroup = Schema?.TableGroups[GroupName]); }
            internal set { cacheGroup = value; }
        }

        public Type ItemType { get; internal set; }

        public IEnumerable<ColumnAttribute> Columns
        {
            get { return cacheColumns; }
        }

        public int BlockSize { get; set; } = 200;

        public bool IsLoging { get; set; } = true;

        public virtual DBTable CreateTable()
        {
            var table = (DBTable)EmitInvoker.CreateObject(typeof(DBTable<>).MakeGenericType(ItemType));
            table.Name = TableName;
            table.Schema = Schema;
            table.DisplayName = ItemType.Name;
            return table;
        }

        public DBTable Generate(DBSchema schema)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));

            Debug.WriteLine($"Generate {TableName}");

            if (TableGroup == null)
            {
                TableGroup = new DBTableGroup(GroupName)
                {
                    Schema = Schema,
                    DisplayName = GroupName
                };
                Schema.TableGroups.Add(TableGroup);
            }

            if (Table == null)
            {
                Table = CreateTable();
            }
            Table.Group = TableGroup;
            Table.Type = TableType;
            Table.BlockSize = BlockSize;
            Table.Sequence = Table.GenerateSequence();

            foreach (var itemType in cacheItemTypes)
            {
                Table.ItemTypes[itemType.Id] = new DBItemType { Type = itemType.Type };
            }

            cacheColumns.Sort((a, b) => a.Order.CompareTo(b.Order));
            foreach (var column in cacheColumns)
            {
                column.Generate();
            }
            foreach (var reference in cacheReferences)
            {
                reference.Generate();
            }
            foreach (var index in cacheIndexes)
            {
                index.Generate();
            }

            if (!Schema.Tables.Contains(TableName))
            {
                Schema.Tables.Add(Table);
            }
            Table.IsLoging = IsLoging;
            return Table;
        }

        public virtual void Initialize(Type type)
        {
            if (ItemType != null)
                return;
            ItemType = type;
            cacheColumns.Clear();
            cacheReferences.Clear();
            cacheIndexes.Clear();
            var types = TypeHelper.GetTypeHierarchi(type);
            foreach (var item in types)
                InitializeType(item);

        }

        public void InitializeItemType(ItemTypeAttribute itemType)
        {
            if (cacheItemTypes.Contains(itemType))
                return;
            cacheItemTypes.Add(itemType);
            InitializeType(itemType.Type);
        }

        public void InitializeType(Type type)
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var column = InitializeColumn(property);
                if (column != null)
                {
                    cacheColumns.Add(column);
                }
                var reference = property.GetCustomAttribute<ReferenceAttribute>();
                if (reference != null)
                {
                    reference.Table = this;
                    reference.Property = property.Name;
                    reference.ReferenceType = property.PropertyType;
                    reference.Column.Keys |= DBColumnKeys.Reference;
                    cacheReferences.Add(reference);
                }
                var index = property.GetCustomAttribute<IndexAttribute>();
                if (index != null)
                {
                    index = cacheIndexes.SelectOne(nameof(IndexAttribute.IndexName), index.IndexName) ?? index;
                    index.Table = this;
                    index.Columns.Add(column);
                    cacheIndexes.Add(index);
                }
            }
        }

        public virtual ColumnAttribute InitializeColumn(PropertyInfo property)
        {
            var column = DBService.GetColumnAttribute(property);
            if (column != null)
            {
                column.Table = this;
                column.Property = property.Name;
                if (column.DataType == null)
                    column.DataType = property.PropertyType;
                if (column.Order <= 0)
                    column.Order = cacheColumns.Count;
            }
            return column;
        }

        public ColumnAttribute GetColumn(string name)
        {
            return cacheColumns.SelectOne(nameof(ColumnAttribute.ColumnName), name);
        }

        public ColumnAttribute GetColumnByProperty(string property)
        {
            return cacheColumns.SelectOne(nameof(ColumnAttribute.Property), property);
        }
    }
}
