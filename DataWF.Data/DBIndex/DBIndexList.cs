using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace DataWF.Data
{
    public class DBIndexList : DBTableItemList<DBIndex>
    {
        public DBIndexList(DBTable table) : base(table)
        {
        }
    }
}
