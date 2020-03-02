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
using System.ComponentModel;

namespace DataWF.Data
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TableAttribute : Attribute
    {
        public TableAttribute(string name, string groupName, string schema = null)
        {
            TableName = name;
            GroupName = groupName ?? "Default";
            Schema = schema;
        }

        public string TableName { get; set; }

        public virtual string GroupName { get; set; }

        public string Schema { get; }

        [DefaultValue(DBTableType.Table)]
        public DBTableType TableType { get; set; } = DBTableType.Table;

        public int BlockSize { get; set; } = 200;

        public bool IsLoging { get; set; } = true;

        public bool ForceLoadReference { get; set; }
    }
}
