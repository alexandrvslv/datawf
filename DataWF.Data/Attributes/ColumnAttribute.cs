/*
 ColumnConfig.cs
 
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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using DataWF.Data;
using DataWF.Common;
using Newtonsoft.Json;
using System.Reflection;
using System.Collections.Generic;

namespace DataWF.Data
{

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public ColumnAttribute()
        { }

        public ColumnAttribute(string name, int size = 0, short scale = 0)
        {
            ColumnName = name;
            Size = size;
            Scale = scale;
        }

        public int Order { get; set; } = -1;

        public string ColumnName { get; set; }

        public string GroupName { get; set; }

        [DefaultValue(DBColumnTypes.Default)]
        public DBColumnTypes ColumnType { get; set; }

        [DefaultValue((int)0)]
        public int Size { get; set; }

        [DefaultValue((short)0)]
        public short Scale { get; set; }

        public DBColumnKeys Keys { get; set; }

        public DBDataType DBDataType { get; set; }

        public Type DataType { get; set; }

        public ColumnAttribute Clone()
        {
            return new ColumnAttribute(ColumnName, Size, Scale)
            {
                GroupName = GroupName,
                ColumnType = ColumnType,
                DBDataType = DBDataType,
                DataType = DataType,
                Keys = Keys
            };
        }
    }
}
