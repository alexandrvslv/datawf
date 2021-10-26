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
using System.Reflection;

[assembly: Invoker(typeof(ReferenceGenerator), nameof(ReferenceGenerator.PropertyName), typeof(ReferenceGenerator.PropertyNameInvoker))]
namespace DataWF.Data
{
    public class ReferenceGenerator
    {
        private ColumnGenerator cacheColumn;
        private DBForeignKey cacheKey;

        public ReferenceGenerator(TableGenerator table, PropertyInfo property, ReferenceAttribute referenceAttribute)
        {
            Attribute = referenceAttribute;
            Table = table;
            PropertyInfo = property;
            ReferenceType = property.PropertyType;
            Column.DisplayName = property.Name;
            Column.Attribute.Keys |= DBColumnKeys.Reference;
            Column.ReferencePropertyInfo = property;
            GenerateName();
        }

        public ReferenceAttribute Attribute { get; set; }

        public Type ReferenceType { get; internal set; }

        public TableGenerator Table { get; internal set; }

        public ColumnGenerator Column => cacheColumn ?? (cacheColumn = Table?.GetColumnByProperty(Attribute.ColumnProperty));

        public DBForeignKey ForeignKey
        {
            get => cacheKey ?? (cacheKey = Table?.Table?.Foreigns[Attribute.Name]);
            internal set { cacheKey = value; }
        }

        public PropertyInfo PropertyInfo { get; set; }

        public string PropertyName => PropertyInfo?.Name;


        public void GenerateName()
        {
            if (string.IsNullOrEmpty(Attribute.Name) && Table != null && Column != null)
            {
                Attribute.Name = $"fk_{Table.Attribute.TableName}_{Column.ColumnName}";
            }
        }

        public DBTable CheckReference()
        {
            var referenceTable = DBTable.GetTable(ReferenceType, Table.Schema, true);
            if (referenceTable == null)
            {
                throw new Exception($"{nameof(ReferenceType)}({Attribute.ColumnProperty} - {ReferenceType}) Table not found! Target table: {Table.Table}");
            }
            if (referenceTable.PrimaryKey == null)
            {
                throw new Exception($"{nameof(ReferenceType)}({Attribute.ColumnProperty} - {ReferenceType}) Primary key not found! Target table: {Table.Table}");
            }
            return referenceTable;
        }

        public virtual DBForeignKey Generate()
        {
            if (ReferenceType == null
                || Table == null || Table.Schema == null
                || Column == null || Column.Column == null)
            {
                throw new Exception($"{nameof(ReferenceAttribute)} is not initialized!");
            }
            var referenceTable = CheckReference();

            if (ForeignKey == null)
            {

                ForeignKey = new DBForeignKey()
                {
                    Table = Table.Table,
                    Column = Column.Column,
                    Reference = referenceTable.PrimaryKey,
                    Name = Attribute.Name,
                };
                Table.Table.Foreigns.Add(ForeignKey);
            }
            Column.Column.IsReference = true;
            ForeignKey.Reference = referenceTable.PrimaryKey;
            ForeignKey.Property = PropertyInfo.Name;
            ForeignKey.PropertyInfo = PropertyInfo;
            if (ForeignKey.Invoker == null)
            {
                ForeignKey.Invoker = EmitInvoker.Initialize(PropertyInfo, true);
            }
            return ForeignKey;
        }

        public class PropertyNameInvoker : Invoker<ReferenceGenerator, string>
        {
            public static readonly PropertyNameInvoker Instance = new PropertyNameInvoker();
            public override string Name => nameof(ReferenceGenerator.PropertyName);

            public override bool CanWrite => false;

            public override string GetValue(ReferenceGenerator target) => target.PropertyName;

            public override void SetValue(ReferenceGenerator target, string value) { }
        }
    }

}