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
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{

    public class DBLogTable<T> : DBTable<T>, IDBLogTable where T : DBLogItem, new()
    {
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
                    userLogkey = Columns[DBLogItem.UserLogKeyName];
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
                Name = value.Name + "_log";
                Schema = value.Schema.LogSchema ?? value.Schema;
                var seqName = value.SequenceName + "_log";
                Sequence = Schema.Sequences[seqName] ?? new DBSequence() { Name = seqName };
                DisplayName = value.DisplayName + " Log";
                var tableGenerator = new LogTableGenerator()
                {
                    Schema = Schema,
                    Table = this,
                    BaseTableAttribute = value.Generator
                };
                tableGenerator.Initialize(typeof(DBLogItem));
                tableGenerator.GenerateColumns();

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
                foreach (var entry in value.ItemTypes)
                {
                    var logEquevalent = TypeHelper.ParseType(entry.Value.Type.Name + "Log");
                    ItemTypes[entry.Key] = new DBItemType { Type = logEquevalent ?? typeof(DBLogItem) };
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

        public DBColumn ParseLogProperty(string name)
        {
            return BaseTable?.ParseProperty(name)?.LogColumn;
        }

        public DBColumn ParseLogProperty(string name, ref DBColumn column)
        {
            if (column != DBColumn.EmptyKey)
                return column;
            return column = BaseTable?.ParseProperty(name)?.LogColumn;
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

        public override async Task<bool> SaveItem(DBItem item, DBTransaction transaction)
        {
            if ((item.UpdateState & DBUpdateState.Delete) == DBUpdateState.Delete
                && item is DBLogItem logItem && FileLOBKey is DBLogColumn logColumn)
            {
                var lob = item.GetValue<uint?>(logColumn);
                var current = logItem.BaseItem == DBItem.EmptyItem ? null : logItem.BaseItem.GetValue<uint?>(logColumn.BaseColumn);
                if (lob != null && lob != current)
                {
                    var qquery = new QQuery(this);
                    qquery.BuildParam(FileLOBKey, lob);
                    if (!Load(qquery).Any(p => p != item))
                    {
                        try
                        {
                            using (var transactionDeleteLOB = new DBTransaction(transaction.DbConnection, transaction.Caller))
                            {
                                await System.DeleteLOB(lob.Value, transactionDeleteLOB);
                                transactionDeleteLOB.Commit();
                            }
                        }
                        catch (Exception ex)
                        {
                            Helper.OnException(ex);
                        }
                    }
                }
            }
            return await base.SaveItem(item, transaction);
        }

        [Invoker(typeof(DBLogTable<>), nameof(BaseTableName))]
        public class BaseTableNameInvoker : Invoker<DBLogTable<T>, string>
        {
            public override string Name => nameof(DBLogTable<T>.BaseTableName);

            public override bool CanWrite => true;

            public override string GetValue(DBLogTable<T> target) => target.BaseTableName;

            public override void SetValue(DBLogTable<T> target, string value) => target.BaseTableName = value;
        }

        [Invoker(typeof(DBLogTable<>), nameof(BaseKey))]
        public class BaseKeyInvoker : Invoker<DBLogTable<T>, DBColumn>
        {
            public override string Name => nameof(DBLogTable<T>.BaseKey);

            public override bool CanWrite => false;

            public override DBColumn GetValue(DBLogTable<T> target) => target.BaseKey;

            public override void SetValue(DBLogTable<T> target, DBColumn value) { }
        }

        [Invoker(typeof(DBLogTable<>), nameof(UserLogKey))]
        public class UserLogKeyInvoker : Invoker<DBLogTable<T>, DBColumn>
        {
            public override string Name => nameof(DBLogTable<T>.UserLogKey);

            public override bool CanWrite => false;

            public override DBColumn GetValue(DBLogTable<T> target) => target.UserLogKey;

            public override void SetValue(DBLogTable<T> target, DBColumn value) { }
        }


    }
}
