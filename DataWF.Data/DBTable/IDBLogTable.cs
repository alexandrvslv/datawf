using System.Collections.Generic;

namespace DataWF.Data
{
    public interface IDBLogTable : IDBTable
    {
        DBColumn BaseKey { get; }
        DBTable BaseTable { get; set; }
        string BaseTableName { get; set; }
        DBColumn UserLogKey { get; }

        DBLogColumn GetLogColumn(DBColumn column);
        IEnumerable<DBLogColumn> GetLogColumns();

        DBColumn ParseLogProperty(string name);
        DBColumn ParseLogProperty(string name, ref DBColumn column);
    }
}