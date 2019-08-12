/*
 Account.cs
 
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
using DataWF.Common;
using System;
using System.Diagnostics;

namespace DataWF.Data
{
    public class ItemTypeGenerator
    {
        private DBTable cacheTable;
        private DBSchema schema;

        public ItemTypeAttribute Attribute { get; set; }

        public Type Type { get; protected set; }

        public TableGenerator TableAttribute { get; set; }

        public IDBVirtualTable VirtualTable { get { return (IDBVirtualTable)Table; } }

        public virtual DBTable Table
        {
            get { return cacheTable ?? (cacheTable = DBService.Schems.ParseTable(Type.Name)); }
            internal set { cacheTable = value; }
        }

        public DBSchema Schema
        {
            get { return schema ?? TableAttribute.Schema; }
            set { schema = value; }
        }

        public virtual void Initialize(Type type)
        {
            Type = type;
            TableAttribute = DBTable.GetTableAttributeInherit(type.BaseType);
            if (TableAttribute == null)
            {
                throw new Exception($"Class with {nameof(ItemTypeAttribute)} must have are {nameof(Type.BaseType)} with {nameof(Data.TableAttribute)}!");
            }
            TableAttribute.InitializeItemType(this);
        }

        public virtual DBTable Generate(DBSchema schema)
        {
            Schema = schema;
            if (Table == null)
            {
                Table = CreateTable();
            }
            Table.Query = $"a.{TableAttribute.Table.ItemTypeKey.SqlName} = {TableAttribute.Table.GetTypeIndex(Type)}";
            if (!Schema.Tables.Contains(Type.Name))
            {
                Schema.Tables.Add(Table);
            }
            Table.TableAttribute = TableAttribute;
            Table.ItemTypeIndex = Attribute.Id;
            VirtualTable.BaseTable = TableAttribute.Table;
            foreach (var columnAttribute in TableAttribute.Columns)
            {
                var virtualColumn = Table.ParseColumn(columnAttribute.ColumnName);
                if (virtualColumn != null)
                {
                    if (columnAttribute.DefaultValues != null && columnAttribute.DefaultValues.TryGetValue(Type, out var defaultValue))
                    {
                        virtualColumn.DefaultValue = defaultValue;
                    }

                    if (virtualColumn.DisplayName.Equals(virtualColumn.Name, StringComparison.Ordinal)
                        || (virtualColumn.DisplayName.Equals(columnAttribute.PropertyInfo.Name, StringComparison.Ordinal)))
                    {
                        virtualColumn.DisplayName = columnAttribute.DisplayName;
                    }
                }
            }

            return Table;
        }

        public virtual DBTable CreateTable()
        {
            if (TableAttribute == null)
            {
                throw new InvalidOperationException("Table attribute not initializes!");
            }
            Debug.WriteLine($"Generate {TableAttribute.Attribute.TableName} - {Type.Name}");

            var table = (DBTable)EmitInvoker.CreateObject(typeof(DBVirtualTable<>).MakeGenericType(Type));
            table.Name = Type.Name;
            table.Schema = Schema;
            ((IDBVirtualTable)table).BaseTable = TableAttribute.Table;
            table.DisplayName = Type.Name;

            return table;
        }
    }
}