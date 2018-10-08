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

using DataWF.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace DataWF.Data
{
    public class ColumnAttributeCache
    {
        private DBColumn cache;

        public ColumnAttributeCache(TableAttributeCache table, PropertyInfo property, ColumnAttribute columnAttribute, CultureInfo culture)
        {
            Attribute = columnAttribute;
            Table = table;
            Culture = culture;
            PropertyName =
                DisplayName = $"{property.Name}{culture.TwoLetterISOLanguageName.ToUpperInvariant()}";
            Property = property.DeclaringType.GetProperty(PropertyName) ?? property;
            ColumnName = $"{columnAttribute.ColumnName}_{culture.TwoLetterISOLanguageName}";
            GroupName = columnAttribute.GroupName ?? columnAttribute.ColumnName;
        }

        public ColumnAttributeCache(TableAttributeCache table, PropertyInfo property, ColumnAttribute columnAttribute)
        {
            Attribute = columnAttribute;
            Table = table;
            Property = property;
            PropertyName =
                DisplayName = property.Name;
            ColumnName = columnAttribute.ColumnName;
            GroupName = columnAttribute.GroupName;
        }

        public Dictionary<Type, string> DefaultValues { get; set; }

        public ColumnAttribute Attribute { get; set; }

        public TableAttributeCache Table { get; set; }

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

        public CultureInfo Culture { get; set; }

        public string DisplayName { get; set; }

        public string GroupName { get; set; }

        public PropertyInfo Property { get; set; }

        public IInvoker PropertyInvoker { get; set; }

        public string PropertyName { get; set; }

        public PropertyInfo ReferenceProperty { get; set; }

        public IInvoker ReferencePropertyInvoker { get; set; }

        public string ColumnName { get; set; }

        public bool IsPrimaryKey => (Attribute.Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary;
        public bool IsTypeKey => (Attribute.Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType;

        public Type GetDataType()
        {
            return TypeHelper.CheckNullable(Attribute.DataType);
        }

        public virtual DBColumn CreateColumn(string name)
        {
            return new DBColumn(name) { Table = Table.Table };
        }

        public DBColumn Generate()
        {
            if (Table == null || Table.Table == null)
                throw new Exception("Table Not Initialized!");

            GenerateColumn(Table.Table);
            return Column;
        }

        public virtual void GenerateColumn(DBTable table)
        {
            if (!string.IsNullOrEmpty(GroupName) && table.ColumnGroups[GroupName] == null)
            {
                var cgroup = new DBColumnGroup(GroupName);
                table.ColumnGroups.Add(cgroup);
            }
            Column = table.Columns[ColumnName];
            if (Column == null)
            {
                Column = CreateColumn(ColumnName);
            }
            if (Column.DisplayName.Equals(Column.Name, StringComparison.Ordinal)
                || (Column.DisplayName.Equals(Property.Name, StringComparison.Ordinal) && ReferenceProperty != null))
            {
                Column.DisplayName = DisplayName;
            }
            Column.Attribute = this;
            Column.DataType = GetDataType();
            Column.Size = Attribute.Size;
            Column.Scale = Attribute.Scale;
            Column.ColumnType = Attribute.ColumnType;
            Column.Keys = Attribute.Keys;
            Column.Property = PropertyName;
            Column.Culture = Culture;
            Column.GroupName = GroupName;
            PropertyInvoker = PropertyName == Property.Name ? EmitInvoker.Initialize(Property, true) : Column;
            ReferencePropertyInvoker = ReferenceProperty == null ? null : EmitInvoker.Initialize(ReferenceProperty, true);
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
