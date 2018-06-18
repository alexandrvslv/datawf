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
using System.Reflection;
using System.Xml.Serialization;
using DataWF.Common;
using Newtonsoft.Json;

namespace DataWF.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public class VirtualTableAttribute : TableAttribute
    {
        public VirtualTableAttribute(string name, Type baseType, string query)
            : base(name, null)
        {
            BaseType = baseType;
            Query = query;
        }

        public override string GroupName
        {
            get { return BaseTable?.GroupName; }
            set { base.GroupName = value; }
        }
        public Type BaseType { get; set; }

        public string Query { get; set; }

        [XmlIgnore, JsonIgnore]
        public TableAttribute BaseTable { get; internal set; }

        public override DBTable CreateTable()
        {
            if (BaseTable == null)
            {
                throw new InvalidOperationException("BaseType of table with table attribute not specified!");
            }
            if (BaseTable.Table == null)
            {
                BaseTable.Generate(Schema);
            }
            var table = (DBTable)EmitInvoker.CreateObject(typeof(DBVirtualTable<>).MakeGenericType(ItemType));
            table.Name = TableName;
            table.Schema = Schema;
            ((IDBVirtualTable)table).BaseTable = BaseTable.Table;
            table.DisplayName = ItemType.Name;
            table.Query = Query;
            return table;
        }

        public override void Initialize(Type type)
        {
            BaseTable = DBTable.GetTableAttribute(BaseType);
            base.Initialize(type);
        }

        public override ColumnAttribute InitializeColumn(PropertyInfo property)
        {
            var column = base.InitializeColumn(property);
            if (column != null && !(column is VirtualColumnAttribute))
            {
                column = new VirtualColumnAttribute(column.ColumnName)
                {
                    Table = column.Table,
                    Property = column.Property,
                    Order = column.Order,
                    Keys = column.Keys,
                    DataType = column.DataType,
                    Default = column.Default,
                    GroupName = column.GroupName
                };
            }
            return column;
        }
    }
}
