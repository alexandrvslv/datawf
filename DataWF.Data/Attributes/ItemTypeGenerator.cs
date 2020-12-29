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
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DataWF.Data
{
    public class ItemTypeGenerator
    {
        private Dictionary<DBSchema, DBTable> generateCache = new Dictionary<DBSchema, DBTable>();

        public ItemTypeAttribute Attribute { get; set; }

        public Type Type { get; protected set; }

        public TableGenerator TableGenerator { get; set; }

        public bool IsGenerated(DBSchema schema, out DBTable table) => generateCache.TryGetValue(schema, out table);

        public virtual void Initialize(Type type)
        {
            Type = type;
            TableGenerator = DBTable.GetTableGeneratorInherit(type.BaseType);
            if (TableGenerator == null)
            {
                throw new Exception($"Class with {nameof(ItemTypeAttribute)} must have are {nameof(Type.BaseType)} with {nameof(Data.TableAttribute)}!");
            }
            TableGenerator.InitializeItemType(this);
        }

        public virtual DBTable Generate(DBSchema schema)
        {
            if (schema == null)
                throw new ArgumentException("Schema is required!", nameof(schema));
            if (IsGenerated(schema, out var table))
                return table;
            table = schema.Tables[Type.Name];
            if (!TableGenerator.IsGenerated(schema, out var baseTable))
            {
                TableGenerator.Generate(schema);
                table = schema.Tables[Type.Name];
                if (table != null)
                    return table;
            }
            if (table == null)
            {
                table = CreateTable(schema, baseTable);
            }
            generateCache[schema] = table;

            table.Query = $"a.{baseTable.ItemTypeKey.SqlName} = {baseTable.GetTypeIndex(Type)}";
            if (!schema.Tables.Contains(Type.Name))
            {
                schema.Tables.Add(table);
            }
            table.Generator = TableGenerator;
            table.ItemTypeIndex = Attribute.Id;
            table.Schema = schema;
            table.RefreshVirtualTable(baseTable);
            foreach (var columnGenerator in TableGenerator.Columns)
            {
                var virtualColumn = table.Columns[columnGenerator.ColumnName];
                if (virtualColumn != null)
                {
                    var baseColumn = baseTable.Columns[columnGenerator.ColumnName];
                    if (virtualColumn.DataType != columnGenerator.DataType)
                    {
                        table.Columns.Add(DBColumnFactory.CreateVirtual(baseColumn, table));
                    }
                    virtualColumn.RefreshVirtualColumn(baseColumn);
                    if (columnGenerator.DefaultValues != null && columnGenerator.DefaultValues.TryGetValue(Type, out var defaultValue))
                    {
                        virtualColumn.DefaultValue = defaultValue;
                    }

                    if (virtualColumn.DisplayName.Equals(virtualColumn.Name, StringComparison.Ordinal)
                        || (virtualColumn.DisplayName.Equals(columnGenerator.PropertyInfo.Name, StringComparison.Ordinal)))
                    {
                        virtualColumn.DisplayName = columnGenerator.DisplayName;
                    }
                }
            }

            return table;
        }

        public virtual DBTable CreateTable(DBSchema schema, DBTable baseTable)
        {
            if (TableGenerator == null)
            {
                throw new InvalidOperationException("Table attribute not initializes!");
            }
            Debug.WriteLine($"Generate {TableGenerator.Attribute.TableName} - {Type.Name}");

            var typeOfTable = Attribute?.Type ?? TypeHelper.ParseType(Type.FullName + "Table") ?? typeof(DBTable<>).MakeGenericType(Type);

            var table = (DBTable)EmitInvoker.CreateObject(typeOfTable);
            table.Name = Type.Name;
            table.Schema = schema;
            table.BaseTable = baseTable;
            table.DisplayName = Type.Name;

            return table;
        }

        public void ClearCache()
        {
            generateCache.Clear();
        }
    }
}