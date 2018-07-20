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
using Newtonsoft.Json;

namespace DataWF.Data
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TableAttribute : Attribute
    {
        static readonly Invoker<ColumnAttribute, string> columnNameInvoker = new Invoker<ColumnAttribute, string>(nameof(ColumnAttribute.ColumnName), (item) => item.ColumnName);
        static readonly Invoker<ColumnAttribute, string> propertyInvoker = new Invoker<ColumnAttribute, string>(nameof(ColumnAttribute.Property), (item) => item.Property.Name);
        static readonly Invoker<IndexAttribute, string> IndexNameinvoker = new Invoker<IndexAttribute, string>(nameof(IndexAttribute.IndexName), (item) => item.IndexName);

        private DBSchema cacheSchema;
        private DBTable cacheTable;
        private DBTableGroup cacheGroup;
        private SelectableList<ColumnAttribute> cacheColumns = new SelectableList<ColumnAttribute>();
        private SelectableList<ReferenceAttribute> cacheReferences = new SelectableList<ReferenceAttribute>();
        private SelectableList<IndexAttribute> cacheIndexes = new SelectableList<IndexAttribute>();



        private SelectableList<ItemTypeAttribute> cacheItemTypes = new SelectableList<ItemTypeAttribute>();
        private List<Type> chacedTypes = new List<Type>();

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

        [XmlIgnore, JsonIgnore]
        public DBSchema Schema
        {
            get { return cacheSchema; }
            set { cacheSchema = value; }
        }

        [XmlIgnore, JsonIgnore]
        public DBTable Table
        {
            get { return cacheTable ?? (cacheTable = DBService.ParseTable(TableName)); }
            internal set { cacheTable = value; }
        }

        [XmlIgnore, JsonIgnore]
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
            return table;
        }

        public DBTable Generate()
        {
            if (Schema == null)
                throw new InvalidOperationException("Can't generate as Schema not defined!");
            return Generate(Schema);
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
            if (Table.DisplayName.Equals(Table.Name, StringComparison.Ordinal))
            {
                Table.DisplayName = ItemType.Name;
            }
            Table.Group = TableGroup;
            Table.Type = TableType;
            Table.BlockSize = BlockSize;
            Table.Sequence = Table.GenerateSequence();

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

            foreach (var itemType in cacheItemTypes)
            {
                Table.ItemTypes[itemType.Id] = new DBItemType { Type = itemType.Type };
                itemType.Generate();
                
            }

            return Table;
        }

        public ColumnAttribute GetPrimaryKey()
        {
            foreach (var column in cacheColumns)
            {
                if ((column.Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary)
                {
                    return column;
                }
            }
            return null;
        }

        public virtual void Initialize(Type type)
        {
            if (ItemType != null)
                return;
            ItemType = type;
            cacheColumns.Clear();
            cacheReferences.Clear();
            cacheIndexes.Clear();
            chacedTypes.Clear();

            var types = TypeHelper.GetTypeHierarchi(type);
            foreach (var item in types)
            {
                InitializeType(item);
            }
        }

        public void InitializeItemType(ItemTypeAttribute itemType)
        {
            if (cacheItemTypes.Contains(itemType))
                return;
            cacheItemTypes.Add(itemType);
            var types = TypeHelper.GetTypeHierarchi(itemType.Type);
            foreach (var item in types)
            {
                InitializeType(item);
            }
        }

        public void InitializeType(Type type)
        {
            if (chacedTypes.Contains(type))
                return;

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (var property in properties)
            {
                if (InitializeColumn(property, out var column))
                {
                    cacheColumns.Add(column);
                }
                if (InitializeIndex(property, column, out var index))
                {
                    cacheIndexes.Add(index);
                }
                InitializeDefault(property);
            }
            foreach (var property in properties)
            {
                if (InitializeReference(property, out var reference))
                {
                    cacheReferences.Add(reference);
                }
            }
            chacedTypes.Add(type);
        }

        private bool InitializeIndex(PropertyInfo property, ColumnAttribute column, out IndexAttribute index)
        {
            index = property.GetCustomAttribute<IndexAttribute>(false);
            if (index != null)
            {
                index = cacheIndexes.SelectOne(nameof(IndexAttribute.IndexName), index.IndexName) ?? index;
                index.Table = this;
                index.Columns.Add(column);
                return true;
            }
            return false;
        }

        public virtual bool InitializeReference(PropertyInfo property, out ReferenceAttribute reference)
        {
            reference = property.GetCustomAttribute<ReferenceAttribute>(false);
            if (reference != null)
            {
                reference.Table = this;
                reference.Property = property;
                reference.ReferenceType = property.PropertyType;
                reference.Column.Keys |= DBColumnKeys.Reference;
                reference.Column.ReferenceProperty = property;
                return true;
            }
            return false;
        }

        public virtual bool InitializeColumn(PropertyInfo property, out ColumnAttribute columnAttribute)
        {
            columnAttribute = property.GetCustomAttribute<ColumnAttribute>(false);
            if (columnAttribute != null)
            {
                columnAttribute.Table = this;
                columnAttribute.Property = property;
                if (columnAttribute.DataType == null)
                    columnAttribute.DataType = property.PropertyType;
                if (columnAttribute.Order <= 0)
                    columnAttribute.Order = cacheColumns.Count;

                return true;
            }
            return false;
        }

        public virtual void InitializeDefault(PropertyInfo property)
        {
            var defaultAttribute = property.GetCustomAttribute<DefaultValueAttribute>(false);
            if (defaultAttribute != null)
            {
                var columnAttribute = GetColumnByProperty(property.Name);
                if (columnAttribute != null)
                {
                    if (columnAttribute.DefaultValues == null)
                        columnAttribute.DefaultValues = new Dictionary<Type, string>();
                    columnAttribute.DefaultValues[property.DeclaringType] = defaultAttribute.Value.ToString();
                }
            }
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
