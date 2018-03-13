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

namespace DataWF.Data
{

    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        [NonSerialized]
        private DBSchema cacheSchema;
        [NonSerialized]
        private DBTable cacheTable;
        [NonSerialized]
        SelectableList<ColumnAttribute> cacheColumns = new SelectableList<ColumnAttribute>();
        [NonSerialized]
        SelectableList<ReferenceAttribute> cacheReferences = new SelectableList<ReferenceAttribute>();
        [NonSerialized]
        SelectableList<IndexAttribute> cacheIndexes = new SelectableList<IndexAttribute>();

        public TableAttribute(string schema, string name)
        {
            cacheColumns.Indexes.Add(new Invoker<ColumnAttribute, string>(nameof(ColumnAttribute.ColumnName), (item) => item.ColumnName));
            cacheColumns.Indexes.Add(new Invoker<ColumnAttribute, string>(nameof(ColumnAttribute.Property), (item) => item.Property));
            cacheIndexes.Indexes.Add(new Invoker<IndexAttribute, string>(nameof(IndexAttribute.IndexName), (item) => item.IndexName));
            SchemaName = schema;
            TableName = name;
        }

        protected string SchemaName { get; set; }

        public string TableName { get; set; }

        [DefaultValue(DBTableType.Table)]
        protected DBTableType TableType { get; set; } = DBTableType.Table;

        [XmlIgnore]
        public DBSchema Schema
        {
            get { return cacheSchema ?? (cacheSchema = DBService.Schems[SchemaName]); }
            internal set
            {
                cacheSchema = value;
                SchemaName = value?.Name;
            }
        }

        [XmlIgnore]
        public DBTable Table
        {
            get { return cacheTable ?? (cacheTable = Schema?.Tables[TableName]); }
            internal set { cacheTable = value; }
        }

        public Type ItemType { get; internal set; }

        public IEnumerable<ColumnAttribute> Columns
        {
            get { return cacheColumns; }
        }

        public int BlockSize { get; set; } = 1000;

        public bool IsLoging { get; set; } = true;

        public virtual DBTable CreateTable()
        {
            var table = (DBTable)EmitInvoker.CreateObject(typeof(DBTable<>).MakeGenericType(ItemType));
            table.Name = TableName;
            table.Schema = Schema;
            table.Type = TableType;
            table.ItemType = ItemType;
            table.BlockSize = BlockSize;
            table.DisplayName = ItemType.Name;
            table.Sequence = table.GenerateSequence(table);
            return table;
        }

        public DBTable Generate(Type type, DBSchema schema)
        {
            Initialize(type);
            if (Schema == null)
            {
                if (schema != null)
                {
                    if (string.IsNullOrEmpty(schema.Name))
                        schema.Name = SchemaName;
                    Schema = schema;
                }
                else
                {
                    Schema = new DBSchema(SchemaName);
                    DBService.Schems.Add(Schema);
                }
            }
            if (Table == null)
            {
                Table = CreateTable();
            }

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

        public void Initialize(Type type)
        {
            if (ItemType != null)
                return;
            ItemType = type;
            cacheColumns.Clear();
            cacheReferences.Clear();
            cacheIndexes.Clear();
            InitializeType(type);
            cacheColumns.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public void InitializeType(Type type)
        {
            foreach (var property in TypeHelper.GetPropertyes(type, false))
            {
                var column = DBService.GetColumnAttribute(property);
                if (column != null)
                {
                    column.Table = this;
                    column.Property = property.Name;
                    if (column.DataType == null)
                        column.DataType = property.PropertyType;
                    if (column.Order == -1)
                        column.Order = cacheColumns.Count;
                    cacheColumns.Add(column);
                }
                var reference = (ReferenceAttribute)GetCustomAttribute(property, typeof(ReferenceAttribute));
                if (reference != null)
                {
                    reference.Table = this;
                    reference.ReferenceType = property.PropertyType;
                    cacheReferences.Add(reference);
                }
                var index = (IndexAttribute)GetCustomAttribute(property, typeof(IndexAttribute));
                if (index != null)
                {
                    index = cacheIndexes.SelectOne(nameof(IndexAttribute.IndexName), index.IndexName) ?? index;
                    index.Table = this;
                    index.Columns.Add(column);
                    cacheIndexes.Add(index);
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
