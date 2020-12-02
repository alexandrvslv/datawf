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

[assembly: Invoker(typeof(ColumnGenerator), nameof(ColumnGenerator.ColumnName), typeof(ColumnGenerator.ColumnNameInvoker))]
[assembly: Invoker(typeof(ColumnGenerator), nameof(ColumnGenerator.PropertyName), typeof(ColumnGenerator.PropertyNameInvoker))]
namespace DataWF.Data
{
    public class ColumnGenerator
    {
        private DBColumn cache;

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

        public virtual DBColumn CreateColumn(string name)
        {
            return DBColumnFactory.Create(DataType, name, table: Table.Table);
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
            if (Column == null || Column.DataType != DataType)
            {
                Column = CreateColumn(ColumnName);
            }
            if (Column.DisplayName.Equals(Column.Name, StringComparison.Ordinal)
                || (Column.DisplayName.Equals(PropertyInfo.Name, StringComparison.Ordinal)
                && ReferencePropertyInfo != null))
            {
                Column.DisplayName = DisplayName;
            }

            if (Attribute.DBDataType != DBDataType.None)
            {
                Column.DBDataType = Attribute.DBDataType;
            }
            Column.Size = Attribute.Size;
            Column.Scale = Attribute.Scale;
            Column.ColumnType = Attribute.ColumnType;
            Column.Keys = Attribute.Keys;
            Column.Culture = Culture;
            Column.GroupName = GroupName;
            Column.PropertyName = PropertyName;
            Column.PropertyInfo = PropertyInfo;
            Column.ReferencePropertyInfo = ReferencePropertyInfo;
            Column.DefaultValues = DefaultValues;
            if (DefaultValues != null && DefaultValues.TryGetValue(PropertyInfo.DeclaringType, out var defaultValue))
            {
                Column.DefaultValue = defaultValue;
            }

            if (!table.Columns.Contains(Column.Name))
            {
                table.Columns.Add(Column);
            }
        }

        public class ColumnNameInvoker : Invoker<ColumnGenerator, string>
        {
            public static readonly ColumnNameInvoker Instance = new ColumnNameInvoker();
            public override string Name => nameof(ColumnGenerator.ColumnName);

            public override bool CanWrite => true;

            public override string GetValue(ColumnGenerator target) => target.ColumnName;

            public override void SetValue(ColumnGenerator target, string value) => target.ColumnName = value;
        }

        public class PropertyNameInvoker : Invoker<ColumnGenerator, string>
        {
            public static readonly PropertyNameInvoker Instance = new PropertyNameInvoker();

            public override string Name => nameof(ColumnGenerator.PropertyName);

            public override bool CanWrite => true;

            public override string GetValue(ColumnGenerator target) => target.PropertyName;

            public override void SetValue(ColumnGenerator target, string value) => target.PropertyName = value;
        }
    }


}
