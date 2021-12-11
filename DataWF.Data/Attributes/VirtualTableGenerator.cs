﻿//  The MIT License (MIT)
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
    public class VirtualTableGenerator
    {
        private Dictionary<IDBSchema, DBTable> generateCache = new Dictionary<IDBSchema, DBTable>();

        public VirtualTableAttribute Attribute { get; set; }

        public Type Type { get; protected set; }

        public TableGenerator TableGenerator { get; set; }

        public bool IsGenerated(IDBSchema schema, out DBTable table) => generateCache.TryGetValue(schema, out table);

        public virtual void Initialize(Type type)
        {
            Type = type;
            TableGenerator = TableGenerator.GetInherit(type.BaseType);
            if (TableGenerator == null)
            {
                throw new Exception($"Class with {nameof(VirtualTableAttribute)} must have are {nameof(Type.BaseType)} with {nameof(Data.TableAttribute)}!");
            }
            TableGenerator.InitializeItemType(this);
        }

        public virtual DBTable Generate(IDBSchema schema)
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

            table.SubQuery = $"a.{baseTable.ItemTypeKey.SqlName} = {baseTable.GetTypeIndex(Type)}";
            if (!schema.Tables.Contains(Type.Name))
            {
                schema.Tables.Add(table);
            }
            table.Generator = TableGenerator;
            table.TypeId = Attribute.Id;
            table.Schema = schema;
            table.RefreshVirtualTable(baseTable);
            RefreshVirtualColumns(table);
            if (table is IDBTableLog logTable)
            {
                var loggedTable = TableGenerator.GetLogTargetTable(schema, Attribute.BaseType);
                loggedTable.LogTable = logTable;
            }
            return table;
        }

        private void RefreshVirtualColumns(DBTable table)
        {
            foreach (var columnGenerator in TableGenerator.Columns)
            {
                var virtualColumn = table.Columns[columnGenerator.ColumnName];
                if (virtualColumn != null)
                {
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
        }

        public virtual DBTable CreateTable(IDBSchema schema, DBTable baseTable)
        {
            if (TableGenerator == null)
            {
                throw new InvalidOperationException("Table attribute not initializes!");
            }
            Debug.WriteLine($"Generate {TableGenerator.Attribute.TableName} - {Type.Name}");

            var typeOfTable = Attribute?.Type 
                ?? TypeHelper.ParseType(Type.FullName + "Table")
                ?? TypeHelper.ParseType(Type.FullName + "Table`1")
                ?? typeof(DBTable<>);

            if (typeOfTable.IsGenericTypeDefinition)
            {
                typeOfTable = typeOfTable.MakeGenericType(Type);
            }

            var table = (DBTable)EmitInvoker.CreateObject(typeOfTable);
            table.IsVirtual = true;
            table.Name = Type.Name;
            table.Schema = schema;
            table.ParentTable = baseTable;
            table.DisplayName = Type.Name;
            if (table is IDBTableLog logTable)
            {
                logTable.TargetTable = TableGenerator.GetLogTargetTable(schema, Attribute.BaseType);
            }
            return table;
        }

        public void ClearCache()
        {
            generateCache.Clear();
        }
    }
}