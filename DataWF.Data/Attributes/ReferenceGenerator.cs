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
using System.Linq;
using System.Reflection;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public partial class ReferenceGenerator
    {
        private ColumnGenerator cacheColumn;
        public ReferenceGenerator(TableGenerator table, PropertyInfo property, ReferenceAttribute referenceAttribute)
        {
            Attribute = referenceAttribute;
            TableGenerator = table;
            PropertyInfo = property;
            ReferenceType = property.PropertyType;
            ColumnGenerator.DisplayName = property.Name;
            ColumnGenerator.Attribute.Keys |= DBColumnKeys.Reference;
            ColumnGenerator.ReferencePropertyInfo = property;
            GenerateName();
        }

        public ReferenceAttribute Attribute { get; set; }

        public Type ReferenceType { get; internal set; }

        public TableGenerator TableGenerator { get; internal set; }

        public ColumnGenerator ColumnGenerator => cacheColumn ?? (cacheColumn = TableGenerator?.GetColumnByProperty(Attribute.ColumnProperty));

        public PropertyInfo PropertyInfo { get; set; }

        public string PropertyName => PropertyInfo?.Name;


        public void GenerateName()
        {
            if (string.IsNullOrEmpty(Attribute.Name) && TableGenerator != null && ColumnGenerator != null)
            {
                Attribute.Name = $"fk_{TableGenerator.Attribute.TableName}_{ColumnGenerator.ColumnName}";
            }
        }

        public virtual DBTable CheckReference(IDBSchema schema)
        {
            if (ReferenceType.IsAbstract)
            {
                var derivedGenerator = TableGenerator.GetDerived(ReferenceType).FirstOrDefault();
                if (derivedGenerator != null)
                {
                    return derivedGenerator.Generate(schema);
                }
                else
                {
                    return null;
                }
            }
            var referenceTable = schema.GetTable(ReferenceType, true);
            if (referenceTable == null)
            {
                throw new Exception($"{nameof(ReferenceType)}({Attribute.ColumnProperty} - {ReferenceType}) Table not found! Target table: {TableGenerator}");
            }
            if (referenceTable.PrimaryKey == null)
            {
                throw new Exception($"{nameof(ReferenceType)}({Attribute.ColumnProperty} - {ReferenceType}) Primary key not found! Target table: {TableGenerator}");
            }
            return referenceTable;
        }

        public virtual DBForeignKey Generate(DBTable table)
        {
            if (ReferenceType == null
                || TableGenerator == null
                || ColumnGenerator == null)
            {
                throw new Exception($"{nameof(ReferenceAttribute)} is not initialized!");
            }

            var referenceTable = CheckReference(table.Schema);
            if (referenceTable == null)
            {
                return null;
            }
            var foreignKey = table?.Foreigns[Attribute.Name];
            var column = table.Columns[ColumnGenerator.ColumnName];
            if (foreignKey == null)
            {
                foreignKey = new DBForeignKey()
                {
                    Table = table,
                    Column = column,
                    Reference = referenceTable.PrimaryKey,
                    Name = Attribute.Name,
                };
                table.Foreigns.Add(foreignKey);
            }
            column.IsReference = true;
            foreignKey.Reference = referenceTable.PrimaryKey;
            foreignKey.Property = PropertyInfo.Name;
            foreignKey.PropertyInfo = PropertyInfo;
            if (foreignKey.Invoker == null)
            {
                foreignKey.Invoker = EmitInvoker.Initialize(PropertyInfo, true);
            }
            return foreignKey;
        }
    }

}