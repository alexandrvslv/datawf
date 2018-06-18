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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IndexAttribute : Attribute
    {
        List<ColumnAttribute> columns = new List<ColumnAttribute>();
        DBIndex cacheIndex;

        public IndexAttribute()
        { }

        public IndexAttribute(string name, bool unique = false)
        {
            IndexName = name;
            Unique = unique;
        }

        public string IndexName { get; set; }

        [DefaultValue(false)]
        public bool Unique { get; set; }

        [XmlIgnore, JsonIgnore]
        public List<ColumnAttribute> Columns
        {
            get { return columns; }
        }

        public DBIndex Index
        {
            get { return cacheIndex ?? (cacheIndex = Table?.Table?.Indexes[IndexName]); }
            internal set { cacheIndex = value; }
        }

        [XmlIgnore, JsonIgnore]
        public TableAttribute Table { get; internal set; }

        public DBIndex Generate()
        {
            if (Index != null)
                return Index;
            Index = new DBIndex()
            {
                Name = IndexName,
                Unique = Unique,
                Table = Table.Table
            };
            foreach (var column in columns)
            {
                Index.Columns.Add(column.Column);
            }
            Table.Table.Indexes.Add(Index);
            return Index;
        }
    }
}
