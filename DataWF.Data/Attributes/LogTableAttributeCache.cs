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
using DataWF.Common;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace DataWF.Data
{
    public class LogTableAttributeCache : TableAttributeCache
    {
        private TableAttributeCache baseTable;

        public LogTableAttribute LogAttribute => base.Attribute as LogTableAttribute;

        public TableAttributeCache BaseTableAttribute
        {
            get => baseTable ?? (baseTable = DBTable.GetTableAttribute(LogAttribute.BaseType));
            set => baseTable = value;
        }

        public override DBSchema Schema
        {
            get => base.Schema is DBLogSchema logSchema
                ? logSchema
                : base.Schema?.LogSchema;
            set => base.Schema = value;
        }

        public override DBTable CreateTable()
        {
            Debug.WriteLine($"Generate Log Table {Attribute.TableName} - {this.ItemType.Name}");

            var type = typeof(DBLogTable<>).MakeGenericType(ItemType);
            var table = (DBTable)EmitInvoker.CreateObject(type);
            table.Name = Attribute.TableName;
            table.Schema = Schema;
            return table;
        }

    }
}
