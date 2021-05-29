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
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DataWF.Data
{
    [InvokerGenerator]
    public partial class RSSchema
    {
        private readonly IListIndex<RSTable> tableNameIndex;

        public RSSchema()
        {
            tableNameIndex = Tables.Indexes.Add(RSTable.TableNameInvoker.Instance);
        }

        public string SchemaName { get; set; }
        [JsonIgnore]
        public DBSchema Schema { get; set; }

        public List<RSTable> IncludeTables { get; set; }

        public List<RSTable> ExcludeTables { get; set; }

        [JsonIgnore]
        public SelectableList<RSTable> Tables { get; set; } = new SelectableList<RSTable>();

        public void Initialize()
        {
            Schema = DBService.Schems[SchemaName];
            if (Schema == null)
                throw new Exception($"Schema with name {SchemaName} not found!");
            Tables.Clear();
            if (IncludeTables != null)
            {
                foreach (var srTable in IncludeTables)
                {
                    srTable.Initialize(this);
                    Tables.Add(srTable);
                }
            }
            else
            {
                foreach (var dbTable in Schema.Tables)
                {
                    if (dbTable.Type != DBTableType.Table
                        || (dbTable.Keys & DBTableKeys.NoReplicate) == DBTableKeys.NoReplicate)
                        continue;
                    var srTable = new RSTable { TableName = dbTable.Name, Table = dbTable };
                    srTable.Initialize(this);
                    Tables.Add(srTable);
                }
            }
        }

        public RSTable GetRSTable(IDBTable table) => tableNameIndex.SelectOne(table.Name);

        public async Task Synch(RSInstance instance)
        {
            foreach (var table in Tables)
            {
                await table.Synch(instance);
            }
        }
    }

    public enum RSQueryType
    {
        SchemaInfo,
        SynchTable,
        SynchSequence,
        SynchFile
    }

    public class RSResult
    {
        public RSQueryType Type { get; set; }
        public object Data { get; set; }

    }

    public class RSSchemaInfo
    {
        public string SchemaName { get; set; }
        public DateTime? Stamp { get; set; }
        public string ObjectName { get; set; }
    }
}
