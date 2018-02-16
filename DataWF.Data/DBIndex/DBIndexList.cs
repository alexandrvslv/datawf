using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace DataWF.Data
{
    public class DBIndexList : DBSchemaItemList<DBIndex>
    {
        public DBIndexList(DBSchema schema)
            : base(schema)
        {
            Indexes.Add(new Invoker<DBIndex, string>(nameof(DBIndex.TableName), (item) => item.TableName));
        }

        public IEnumerable<DBIndex> GetByTable(DBTable table)
        {
            return Select(nameof(DBIndex.TableName), CompareType.Equal, table.FullName);
        }
    }
}
