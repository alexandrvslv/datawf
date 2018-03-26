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

namespace DataWF.Data
{

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        [NonSerialized]
        private DBColumn cache;

        public ColumnAttribute()
        { }

        public ColumnAttribute(string name, int size = 0, short scale = 0, string def = null)
        {
            ColumnName = name;
            Size = size;
            Scale = scale;
            Default = def;
        }

        public int Order { get; set; }

        public string ColumnName { get; set; }

        public string GroupName { get; set; }

        [DefaultValue(DBColumnTypes.Default)]
        public DBColumnTypes ColumnType { get; set; }

        [DefaultValue((int)0)]
        public int Size { get; set; }

        [DefaultValue((short)0)]
        public short Scale { get; set; }

        public string Default { get; set; }

        public DBColumnKeys Keys { get; set; }

        [XmlIgnore]
        public TableAttribute Table { get; internal set; }

        [XmlIgnore]
        public DBColumn Column
        {
            get
            {
                if (cache == null)
                    cache = Table?.Table?.Columns[ColumnName];
                return cache;
            }
            internal set { cache = value; }
        }

        [XmlIgnore]
        public string Property { get; internal set; }

        [XmlIgnore]
        public Type DataType { get; set; }


        public virtual DBColumn CreateColumn(string name)
        {
            var type = DataType;
            if (DataType.IsGenericType)
                type = DataType.GetGenericArguments()?.FirstOrDefault();
            return new DBColumn(name, type, Size)
            {
                Scale = Scale,
                ColumnType = ColumnType,
                DefaultValue = Default,
                Keys = Keys,
                Table = Table.Table,
                DisplayName = Property,
                GroupName = GroupName
            };
        }

        public DBColumn Generate()
        {
            if (Table == null || Table.Table == null)
                throw new Exception("Table Not Initialized!");
            if (Column != null)
                return Column;
            if (!string.IsNullOrEmpty(GroupName))
            {
                var cgroup = Table.Table.ColumnGroups[GroupName];
                if (cgroup == null)
                {
                    Table.Table.ColumnGroups.Add(new DBColumnGroup(GroupName));
                }
            }
            if ((Keys & DBColumnKeys.Culture) == DBColumnKeys.Culture)
            {
                foreach (var culture in Locale.Instance.Cultures)
                {
                    GenerateCultureColumn(Table.Table, culture);
                }
            }
            else
            {
                Column = CreateColumn(ColumnName);
                Table.Table.Columns.Add(Column);
            }
            return Column;
        }

        public void GenerateCultureColumn(DBTable table, CultureInfo culture)
        {
            if (table.ColumnGroups[ColumnName] == null)
            {
                var cgroup = new DBColumnGroup(ColumnName);
                table.ColumnGroups.Add(cgroup);
            }
            string name = ColumnName + "_" + culture.TwoLetterISOLanguageName;

            if (!table.Columns.Contains(name))
            {
                Column = CreateColumn(name);
                Column.DisplayName = $"{Column.DisplayName} {culture.TwoLetterISOLanguageName.ToUpperInvariant()}";
                Column.GroupName = ColumnName;
                Column.Culture = culture;
                Table.Table.Columns.Add(Column);
            }
        }
    }
}
