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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace DataWF.Data
{
    public class LogTableGenerator : TableGenerator
    {
        private TableGenerator baseTable;

        public LogTableAttribute LogAttribute => base.Attribute as LogTableAttribute;

        public TableGenerator BaseTableGenerator
        {
            get => baseTable ?? (baseTable = TableGenerator.Get(LogAttribute.BaseType));
            set => baseTable = value;
        }

        public override DBTable CreateTable(IDBSchema schema)
        {
            Debug.WriteLine($"Generate Log Table {Attribute.TableName} - {this.ItemType.Name}");

            var type = Attribute.Type
                ?? (ItemType.IsSealed
                    ? TypeHelper.ParseType(ItemType.FullName + "Table")
                    : TypeHelper.ParseType(ItemType.FullName + "Table`1"))
                ?? typeof(DBTableLog<>);
            if (type.IsGenericTypeDefinition)
            {
                type = type.MakeGenericType(ItemType);
            }
            var table = (IDBTableLog)EmitInvoker.CreateObject(type);
            table.Name = Attribute.TableName;
            table.Schema = schema;
            var baseSchema = schema is DBLogSchema logSchema ? logSchema.BaseSchema : schema;
            table.TargetTable = baseSchema.GetTable(LogAttribute.BaseType);
            return (DBTable)table;
        }

        public override DBTable Generate(IDBSchema schema)
        {
            var table = base.Generate(schema);
            table.SetItemType(ItemType);
            return table;
        }
    }
}
