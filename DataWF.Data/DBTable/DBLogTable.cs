/*
 DBTable.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using DataWF.Common;
using Newtonsoft.Json;

namespace DataWF.Data
{

    public class DBLogTable : DBTable<DBLogItem>
    {
        public static readonly string TypeName = "logtype";
        public static readonly string IdName = "logid";
        public static readonly string DateName = "datecreate";
        public static readonly string StateName = "status_id";
        public static readonly string UserLogName = "userlog_id";
        public static DBTable UserLogTable { get; set; }

        private DBTable baseTable;
        private DBColumn basekey = DBColumn.EmptyKey;
        private DBColumn userLogkey = DBColumn.EmptyKey;

        public DBLogTable()
        { }

        public DBLogTable(DBTable table)
        {
            BaseTable = table;
        }

        public DBColumn BaseKey
        {
            get
            {
                if (basekey == DBColumn.EmptyKey && BaseTable != null)
                {
                    basekey = GetLogColumn(BaseTable.PrimaryKey);
                }
                return basekey;
            }
        }

        public DBColumn UserLogKey
        {
            get
            {
                if (userLogkey == DBColumn.EmptyKey)
                {
                    userLogkey = Columns[UserLogName];
                }
                return userLogkey;
            }
        }

        public string BaseTableName { get; set; }

        [XmlIgnore, JsonIgnore]
        public DBTable BaseTable
        {
            get
            {
                return baseTable ?? (baseTable = Schema is DBLogSchema
                              ? ((DBLogSchema)Schema).BaseSchema.Tables[BaseTableName]
                              : Schema?.Tables[BaseTableName]);
            }
            set
            {
                baseTable = value ?? throw new ArgumentException("BaseTable set operation required not null value!");
                BaseTableName = value.Name;
                block = 50;
                Name = value.Name + "_log";
                Schema = value.Schema.LogSchema ?? value.Schema;
                var seqName = value.SequenceName + "_log";
                Sequence = Schema.Sequences[seqName] ?? new DBSequence() { Name = seqName };
                DisplayName = value.DisplayName + " Log";

                var idColumn = Columns[IdName];
                if (idColumn == null) Columns.Add(new DBColumn() { Name = IdName, DataType = typeof(int), Keys = DBColumnKeys.Primary | DBColumnKeys.Notnull });
                else { idColumn.DataType = typeof(int); idColumn.Keys = DBColumnKeys.Primary | DBColumnKeys.Notnull; }
                var dateColumn = Columns[DateName];
                if (dateColumn == null) Columns.Add(new DBColumn() { Name = DateName, DataType = typeof(DateTime), Keys = DBColumnKeys.Date });
                else { dateColumn.DataType = typeof(DateTime); dateColumn.Keys = DBColumnKeys.Date; }
                var typeColumn = Columns[TypeName];
                if (typeColumn == null) Columns.Add(new DBColumn() { Name = TypeName, DataType = typeof(DBLogType), Keys = DBColumnKeys.ElementType });
                else { typeColumn.DataType = typeof(DBLogType); typeColumn.Keys = DBColumnKeys.ElementType; }
                var stateColumn = Columns[StateName];
                if (stateColumn == null) Columns.Add(new DBColumn() { Name = StateName, DataType = typeof(DBStatus), Keys = DBColumnKeys.State, DefaultValue = "2" });
                else { stateColumn.DataType = typeof(DBStatus); stateColumn.Keys = DBColumnKeys.State; stateColumn.DefaultValue = "2"; }
                var userColumn = Columns[UserLogName];
                if (userColumn == null) Columns.Add(new DBColumn() { Name = UserLogName, DataType = typeof(long) });
                else { userColumn.DataType = typeof(long); userColumn.Keys = DBColumnKeys.None; }

                foreach (var column in value.Columns)
                {
                    if (column.ColumnType == DBColumnTypes.Default
                        && (column.Keys & DBColumnKeys.NoLog) != DBColumnKeys.NoLog)
                    {
                        var logColumn = GetLogColumn(column);
                        if (logColumn == null)
                        {
                            logColumn = new DBLogColumn(column);
                            Columns.Add(logColumn);
                        }
                        else
                        {
                            logColumn.BaseColumn = column;
                        }
                    }
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public override AccessValue Access
        {
            get { return BaseTable.Access; }
            set { base.Access = value; }
        }

        public DBLogColumn GetLogColumn(DBColumn column)
        {
            return ParseColumn(column.Name + "_log") as DBLogColumn;
        }

        public IEnumerable<DBLogColumn> GetLogColumns()
        {
            foreach (var column in Columns)
            {
                if (column is DBLogColumn)
                {
                    yield return (DBLogColumn)column;
                }
            }
        }


    }
}
