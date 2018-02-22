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
using System.Xml.Serialization;
using DataWF.Common;

namespace DataWF.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public class VirtualTableAttribute : TableAttribute
    {
        public VirtualTableAttribute(string schema, string name, Type baseType, string query)
            : base(schema, name)
        {
            BaseType = baseType;
            Query = query;
            BaseTable = DBService.GetTableAttribute(baseType);
        }

        public Type BaseType { get; set; }

        public string Query { get; set; }

        [XmlIgnore]
        public TableAttribute BaseTable { get; internal set; }

        public override DBTable CreateTable()
        {
            if (BaseTable == null)
            {
                throw new InvalidOperationException("BaseType of table with table attribute not specified!");
            }
            if (BaseTable.Table == null)
            {
                BaseTable.Generate(BaseType, Schema);
            }
            var table = (DBTable)EmitInvoker.CreateObject(typeof(DBVirtualTable<>).MakeGenericType(ItemType));
            table.Name = TableName;
            table.Schema = Schema;
            ((IDBVirtualTable)table).BaseTable = BaseTable.Table;
            table.Query = Query;
            return table;
        }
    }
}
