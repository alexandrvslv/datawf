//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public partial class ColumnGenerator
    {
        public ColumnGenerator(TableGenerator table, PropertyInfo property, ColumnAttribute columnAttribute, CultureInfo culture)
        {
            Attribute = columnAttribute;
            Table = table;
            Culture = culture;
            GroupName = columnAttribute.GroupName ?? columnAttribute.ColumnName;
            PropertyName =
                DisplayName = $"{property.Name}{culture.TwoLetterISOLanguageName.ToUpperInvariant()}";
            PropertyInfo = property.DeclaringType.GetProperty(PropertyName) ?? property;
            ColumnName = $"{columnAttribute.ColumnName}_{culture.TwoLetterISOLanguageName}";
        }

        public ColumnGenerator(TableGenerator table, PropertyInfo property, ColumnAttribute columnAttribute)
        {
            Attribute = columnAttribute;
            Table = table;
            PropertyInfo = property;
            PropertyName =
                DisplayName = property.Name;
            ColumnName = columnAttribute.ColumnName;
            GroupName = columnAttribute.GroupName;
        }

        public ColumnAttribute Attribute { get; set; }

        public TableGenerator Table { get; set; }

        public CultureInfo Culture { get; set; }

        public string DisplayName { get; set; }

        public string GroupName { get; set; }

        public PropertyInfo PropertyInfo { get; set; }

        public string PropertyName { get; set; }

        public PropertyInfo ReferencePropertyInfo { get; set; }

        public string ColumnName { get; set; }

        public bool IsPrimaryKey => (Attribute.Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary;

        public bool IsTypeKey => (Attribute.Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType;

        public Dictionary<Type, string> DefaultValues { get; set; }

        public virtual int Size => Attribute?.Size ?? 0;

        public Type DataType
        {
            get => Attribute.DataType;
        }

        public virtual DBColumn CreateColumn(DBTable table, string name)
        {
            return DBColumnFactory.Create(DataType, name, table: table);
        }

        public DBColumn Generate(DBTable table)
        {
            if (Table == null || table == null)
                throw new Exception("Table Not Initialized!");

            return GenerateColumn(table);
        }

        public virtual DBColumn GenerateColumn(DBTable table)
        {
            if (!string.IsNullOrEmpty(GroupName) && table.ColumnGroups[GroupName] == null)
            {
                var cgroup = new DBColumnGroup(GroupName);
                table.ColumnGroups.Add(cgroup);
            }
            var column = table.Columns[ColumnName];
            if (column == null || column.DataType != DataType)
            {
                column = CreateColumn(table, ColumnName);
            }

            //|| (Column.DisplayName.Equals(PropertyInfo.Name, StringComparison.Ordinal)
            //&& ReferencePropertyInfo != null)
            if (string.Equals(column.DisplayName, column.Name, StringComparison.Ordinal))
            {
                column.DisplayName = DisplayName;
            }

            if (Attribute.DBDataType != DBDataType.None)
            {
                column.DBDataType = Attribute.DBDataType;
            }
            column.Size = Attribute.Size;
            column.Scale = Attribute.Scale;
            column.ColumnType = Attribute.ColumnType;
            column.Keys = Attribute.Keys;
            column.Culture = Culture;
            column.GroupName = GroupName;
            column.PropertyName = PropertyName;
            column.PropertyInfo = PropertyInfo;
            column.ReferencePropertyInfo = ReferencePropertyInfo;
            column.DefaultValues = DefaultValues;
            if (DefaultValues != null && DefaultValues.TryGetValue(PropertyInfo.DeclaringType, out var defaultValue))
            {
                column.DefaultValue = defaultValue;
            }

            if (!table.Columns.Contains(column))
            {
                table.Columns.Add(column);
            }
            return column;
        }
    }


}
