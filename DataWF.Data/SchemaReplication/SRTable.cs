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
using System.Text.Json.Serialization;

[assembly: Invoker(typeof(SRTable), nameof(SRTable.TableName), typeof(SRTable.TableNameInvoker))]
[assembly: Invoker(typeof(SRTable), nameof(SRTable.Table), typeof(SRTable.TableInvoker))]
namespace DataWF.Data
{
    public class SRTable
    {
        public string TableName { get; set; }

        [JsonIgnore]
        public DBTable Table { get; set; }


        public void Initialize(SRSchema schema)
        {
            Table = schema.Schema.Tables[TableName];
            if (Table == null)
                throw new Exception($"Table with name {TableName} not found on schema {schema.SchemaName}");

        }

        public class TableNameInvoker : Invoker<SRTable, string>
        {
            public override string Name => nameof(TableName);

            public override bool CanWrite => true;

            public override string GetValue(SRTable target) => target.TableName;

            public override void SetValue(SRTable target, string value) => target.TableName = value;
        }

        public class TableInvoker : Invoker<SRTable, DBTable>
        {
            public static readonly TableInvoker Instance = new TableInvoker();

            public override string Name => nameof(Table);

            public override bool CanWrite => true;

            public override DBTable GetValue(SRTable target) => target.Table;

            public override void SetValue(SRTable target, DBTable value) => target.Table = value;
        }

    }
}
