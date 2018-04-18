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

namespace DataWF.Data
{

    public class DBLogTable : DBTable<DBLogItem>
    {
        public static readonly string UserLogName = "userlog_id";
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
                    basekey = Columns[UserLogName];
                }
                return basekey;
            }
        }

        public string BaseTableName { get; set; }

        [XmlIgnore]
        public DBTable BaseTable
        {
            get { return baseTable ?? (baseTable = Schema?.Tables[BaseTableName]); }
            set
            {
                //if (BaseTable != null || value == null)
                //    throw new Exception("Log table Initialized!");
                baseTable = value;
                BaseTableName = value.Name;

                Name = value.Name + "_log";
                Schema = value.Schema;
                var seqName = value.SequenceName + "_log";
                Sequence = value.Schema.Sequences[seqName] ?? new DBSequence() { Name = seqName };

                if (!Columns.Contains("logid"))
                {
                    Columns.Add(new DBColumn()
                    {
                        Name = "logid",
                        DataType = typeof(int),
                        Keys = DBColumnKeys.Primary | DBColumnKeys.Notnull
                    });
                    Columns.Add(new DBColumn()
                    {
                        Name = "datecreate",
                        DataType = typeof(DateTime),
                        Keys = DBColumnKeys.Date
                    });
                    Columns.Add(new DBColumn()
                    {
                        Name = "logtype",
                        DataType = typeof(DBLogType),
                        Keys = DBColumnKeys.ElementType
                    });
                    Columns.Add(new DBColumn()
                    {
                        Name = UserLogName,
                        DataType = typeof(DBLogType),
                        Keys = DBColumnKeys.ElementType
                    });
                }
                foreach (var column in value.Columns)
                {
                    if (column.ColumnType == DBColumnTypes.Default
                        && (column.Keys & DBColumnKeys.NoLog) != DBColumnKeys.NoLog
                        && !Columns.Contains(DBLogColumn.GetName(column)))
                    {
                        Columns.Add(new DBLogColumn(column));
                    }
                }
            }
        }

        [XmlIgnore]
        public override AccessValue Access
        {
            get { return BaseTable.Access; }
            set { base.Access = value; }
        }

        private DBLogColumn GetLogColumn(DBColumn column)
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
