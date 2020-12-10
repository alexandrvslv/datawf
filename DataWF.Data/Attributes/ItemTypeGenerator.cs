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
using System.Diagnostics;

namespace DataWF.Data
{
    public class ItemTypeGenerator
    {
        private DBTable cacheTable;
        private DBSchema schema;

        public ItemTypeAttribute Attribute { get; set; }

        public Type Type { get; protected set; }

        public TableGenerator TableGenerator { get; set; }

        public IDBVirtualTable VirtualTable { get { return (IDBVirtualTable)Table; } }

        public virtual DBTable Table
        {
            get => cacheTable ?? (cacheTable = Schema?.Tables[Type.Name] ?? DBService.Schems.ParseTable(Type.Name));
            internal set => cacheTable = value;
        }

        public bool Generated { get; protected set; }

        public DBSchema Schema
        {
            get => schema ?? TableGenerator.Schema;
            set => schema = value;
        }

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
            Generated = true;
            Schema = schema;
            if (!TableGenerator.Generated)
            {
                TableGenerator.Generate(schema);
                if (Table != null)
                    return Table;
            }
            if (Table == null)
            {
                Table = CreateTable();
            }
            Table.Query = $"a.{TableGenerator.Table.ItemTypeKey.SqlName} = {TableGenerator.Table.GetTypeIndex(Type)}";
            if (!Schema.Tables.Contains(Type.Name))
            {
                Schema.Tables.Add(Table);
            }
            Table.Generator = TableGenerator;
            Table.ItemTypeIndex = Attribute.Id;
            Table.Schema = schema;
            VirtualTable.BaseTable = TableGenerator.Table;
            VirtualTable.Generate();
            foreach (var columnGenerator in TableGenerator.Columns)
            {
                var virtualColumn = Table.Columns[columnGenerator.ColumnName];
                if (virtualColumn != null)
                {
                    if (virtualColumn.GetType() != columnGenerator.Column.GetType())
                    {
                        Table.Columns.Add(DBColumnFactory.CreateVirtual(columnGenerator.Column, VirtualTable));
                    }
                    virtualColumn.RefreshVirtualColumn(columnGenerator.Column);
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

            return Table;
        }

        public virtual DBTable CreateTable()
        {
            if (TableGenerator == null)
            {
                throw new InvalidOperationException("Table attribute not initializes!");
            }
            Debug.WriteLine($"Generate {TableGenerator.Attribute.TableName} - {Type.Name}");

            var table = (DBTable)EmitInvoker.CreateObject(typeof(DBVirtualTable<>).MakeGenericType(Type));
            table.Name = Type.Name;
            table.Schema = Schema;
            ((IDBVirtualTable)table).BaseTable = TableGenerator.Table;
            table.DisplayName = Type.Name;

            return table;
        }
    }
}