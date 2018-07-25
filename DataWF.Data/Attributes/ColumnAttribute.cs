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
        [NonSerialized]
        private DBColumn cache;

        public ColumnAttribute()
        { }

        public ColumnAttribute(string name, int size = 0, short scale = 0)
        {
            ColumnName = name;
            Size = size;
            Scale = scale;
        }

        public int Order { get; set; }

        public Dictionary<Type, string> DefaultValues { get; internal set; }

        public string ColumnName { get; set; }

        public string GroupName { get; set; }

        [DefaultValue(DBColumnTypes.Default)]
        public DBColumnTypes ColumnType { get; set; }

        [DefaultValue((int)0)]
        public int Size { get; set; }

        [DefaultValue((short)0)]
        public short Scale { get; set; }

        public DBColumnKeys Keys { get; set; }

        [XmlIgnore, JsonIgnore]
        public TableAttribute Table { get; internal set; }

        [XmlIgnore, JsonIgnore]
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

        [XmlIgnore, JsonIgnore]
        public PropertyInfo Property { get; set; }

        [XmlIgnore, JsonIgnore]
        public PropertyInfo ReferenceProperty { get; internal set; }

        [XmlIgnore, JsonIgnore]
        public Type DataType { get; set; }

       public Type GetDataType()
        {
            var type = DataType;
            if (DataType.IsGenericType)
                type = DataType.GetGenericArguments()?.FirstOrDefault();
            return type;
        }

        public virtual DBColumn CreateColumn(string name)
        {
            return new DBColumn(name) { Table = Table.Table };
        }

        public DBColumn Generate()
        {
            if (Table == null || Table.Table == null)
                throw new Exception("Table Not Initialized!");
            if ((Keys & DBColumnKeys.Culture) == DBColumnKeys.Culture)
            {
                foreach (var culture in Locale.Instance.Cultures)
                {
                    GenerateColumn(Table.Table, ColumnName, culture);
                }
            }
            else
            {
                GenerateColumn(Table.Table, GroupName, null);
            }
            return Column;
        }

        public virtual void GenerateColumn(DBTable table, string groupName, CultureInfo culture)
        {
            if (!string.IsNullOrEmpty(groupName) && table.ColumnGroups[groupName] == null)
            {
                var cgroup = new DBColumnGroup(groupName);
                table.ColumnGroups.Add(cgroup);
            }
            string name = culture == null ? ColumnName : $"{ColumnName}_{culture.TwoLetterISOLanguageName}";
            Column = table.Columns[name];
            if (Column == null)
            {
                Column = CreateColumn(name);
            }
            if (Column.DisplayName.Equals(Column.Name, StringComparison.Ordinal)
                || (Column.DisplayName.Equals(Property.Name, StringComparison.Ordinal) && ReferenceProperty != null))
            {
                Column.DisplayName = culture == null ? ReferenceProperty?.Name ?? Property.Name : $"{Property.Name} {culture.TwoLetterISOLanguageName.ToUpperInvariant()}";
            }
            Column.DataType = GetDataType();
            Column.Size = Size;
            Column.Scale = Scale;
            Column.ColumnType = ColumnType;
            Column.Keys = Keys;
            Column.Property = culture == null ? Property.Name : $"{Property.Name}{culture.TwoLetterISOLanguageName.ToUpper()}";
            Column.PropertyInvoker = culture == null //TODO Check if property with culture defined in class
                ? EmitInvoker.Initialize(Property, false) : Column;
            Column.Culture = culture;
            Column.GroupName = groupName;
            Column.ReferenceProperty = ReferenceProperty == null ? null : EmitInvoker.Initialize(ReferenceProperty, false);
            if (DefaultValues != null && DefaultValues.TryGetValue(Property.DeclaringType, out var defaultValue))
            {
                Column.DefaultValue = defaultValue;
            }

            if (!table.Columns.Contains(Column.Name))
            {
                table.Columns.Add(Column);
            }
        }
    }
}
