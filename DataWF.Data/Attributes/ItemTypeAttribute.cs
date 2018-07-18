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

namespace DataWF.Data
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ItemTypeAttribute : Attribute
    {
        private DBTable cacheTable;

        public ItemTypeAttribute(int id)
        {
            Id = id;
        }

        public int Id { get; private set; }

        public Type Type { get; private set; }

        public string Query { get; set; }

        public TableAttribute TableAttribute { get; private set; }

        public DBTable Table
        {
            get { return cacheTable ?? (cacheTable = DBService.ParseTable(Type.Name)); }
            internal set { cacheTable = value; }
        }

        public void Initialize(Type type)
        {
            Type = type;
            do
            {
                type = type.BaseType;
                TableAttribute = type == null ? null : DBTable.GetTableAttribute(type);
            }
            while (TableAttribute == null && type != null);
            if (TableAttribute == null)
            {
                throw new Exception($"Class with {nameof(ItemTypeAttribute)} must have are {nameof(Type.BaseType)} with {nameof(Data.TableAttribute)}!");
            }
            TableAttribute.InitializeItemType(this);
        }

        public DBTable Generate()
        {
            if (Table == null)
                Table = CreateTable();
            if (!TableAttribute.Schema.Tables.Contains(Type.Name))
            {
                TableAttribute.Schema.Tables.Add(Table);
            }
            return Table;
        }

        public DBTable CreateTable()
        {
            if (TableAttribute == null)
            {
                throw new InvalidOperationException("Table attribute not initializes!");
            }
            if (TableAttribute.Table == null)
            {
                TableAttribute.Generate();
            }
            var table = (DBTable)EmitInvoker.CreateObject(typeof(DBVirtualTable<>).MakeGenericType(Type));
            table.Name = Type.Name;
            table.Schema = TableAttribute.Schema;
            ((IDBVirtualTable)table).BaseTable = TableAttribute.Table;
            table.DisplayName = Type.Name;
            table.Query = $"{TableAttribute.Table.ItemTypeKey.SqlName} = {TableAttribute.Table.GetTypeIndex(Type)}";
            return table;
        }
    }
}