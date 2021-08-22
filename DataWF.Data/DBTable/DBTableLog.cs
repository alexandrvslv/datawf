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
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public partial class DBTableLog<T> : DBTable<T>, IDBTableLog where T : DBItemLog
    {
        private IDBTable targetTable;
        private string targetTableName;
        private DBColumn basekey = DBColumn.EmptyKey;
        private DBColumn userLogkey = DBColumn.EmptyKey;

        public DBTableLog()
        { }

        public DBTableLog(IDBTable table)
        {
            TargetTable = table;
        }

        [XmlIgnore, JsonIgnore]
        public DBColumn BaseKey
        {
            get
            {
                if (basekey == DBColumn.EmptyKey && TargetTable != null)
                {
                    basekey = GetLogColumn(TargetTable.PrimaryKey);
                }
                return basekey;
            }
        }

        [XmlIgnore, JsonIgnore]
        public DBColumn UserLogKey
        {
            get
            {
                if (userLogkey == DBColumn.EmptyKey)
                {
                    userLogkey = Columns[DBItemLog.UserLogKeyName];
                }
                return userLogkey;
            }
        }

        public string TargetTableName
        {
            get => targetTableName;
            set
            {
                if (!string.Equals(targetTableName, value, StringComparison.Ordinal))
                {
                    targetTableName = value;
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public IDBTable TargetTable
        {
            get => targetTable ??= (Schema is DBSchemaLog logSchema
                              ? logSchema.TargetSchema.Tables[TargetTableName]
                              : Schema?.Tables[TargetTableName]);
            set
            {
                TargetTableName = value?.Name ?? throw new ArgumentException("Value is required", nameof(value));
                targetTable = value;
                Name = $"{value.Name}{(IsVirtual ? "Log" : "_log")}";
                Schema = value.Schema.LogSchema ?? value.Schema;
                var seqName = $"{value.SequenceName}_log";
                Sequence = Schema.Sequences[seqName] ?? new DBSequence() { Name = seqName };
                DisplayName = $"{value.DisplayName} Log";


                foreach (var column in value.Columns)
                {
                    if (column.ColumnType == DBColumnTypes.Default
                        && (column.Keys & DBColumnKeys.NoLog) != DBColumnKeys.NoLog)
                    {
                        var logColumn = GetLogColumn(column);
                        if (logColumn == null)
                        {
                            logColumn = DBColumnFactory.CreateLog(column, this);
                            Columns.Add(logColumn);
                        }
                        else
                        {
                            logColumn.RefreshLogColumn(column);
                        }
                    }
                }
                foreach (var entry in value.ItemTypes)
                {
                    var logEquevalent = TypeHelper.ParseType(entry.Value.Type.Name + "Log");
                    ItemTypes[entry.Key] = new DBItemType { Type = logEquevalent ?? typeof(DBItemLog) };
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public override AccessValue Access
        {
            get { return access ?? TargetTable.Access; }
            set { base.Access = value; }
        }

        public DBColumn GetLogColumn(DBColumn column)
        {
            return Columns.GetByTarget(column.Name) ?? Columns[$"{column.Name}_log"];
        }

        public DBColumn ParseLogProperty(string name)
        {
            return TargetTable?.ParseProperty(name)?.LogColumn;
        }

        public DBColumn ParseLogProperty(string name, ref DBColumn column)
        {
            if (column != DBColumn.EmptyKey)
                return column;
            return column = TargetTable?.ParseProperty(name)?.LogColumn;
        }

        public IEnumerable<DBColumn> GetLogColumns()
        {
            foreach (var column in Columns)
            {
                if (column.TargetColumn != null)
                {
                    yield return column;
                }
            }
        }

        public override async Task<bool> SaveItem(DBItem item, DBTransaction transaction)
        {
            if ((item.UpdateState & DBUpdateState.Delete) == DBUpdateState.Delete
                && item is DBItemLog logItem && FileOIDKey != null
                && !transaction.Replication)
            {
                var lob = item.GetValue(FileOIDKey);
                var current = logItem.BaseItem == DBItem.EmptyItem ? null : logItem.BaseItem.GetValue((DBColumn<long?>)FileOIDKey.TargetColumn);
                if (lob != null && lob != current)
                {
                    var qquery = new QQuery(this);
                    qquery.BuildParam(FileOIDKey, lob);
                    if (!Load(qquery).Any(p => p != item))
                    {
                        try
                        {
                            using (var transactionDeleteLOB = new DBTransaction(transaction.DbConnection, transaction.Caller))
                            {
                                await System.DeleteBlob(lob.Value, transactionDeleteLOB);
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

        public override void RemoveDeletedColumns()
        {
            base.RemoveDeletedColumns();
            for (int i = 0; i < Columns.Count;)
            {
                var column = Columns[i];
                if (column.TargetName != null && column.TargetColumn == null)
                {
                    column.RemoveConstraints();
                    column.RemoveForeignKeys();
                    column.RemoveIndexes();

                    Columns.RemoveInternal(column, i);
                }
                else
                {
                    i++;
                }
            }
        }

        public static async Task Reject(IEnumerable<T> redo, IUserIdentity user)
        {
            var changed = new Dictionary<DBItem, List<T>>();
            foreach (T log in redo.OrderBy(p => p.PrimaryId))
            {
                DBItem row = log.BaseItem;
                if (row == null)
                {
                    if (log.LogType == DBLogType.Insert)
                        continue;
                    row = log.BaseTable.NewItem(DBUpdateState.Insert, false);
                    row.SetValue(log.BaseId, log.BaseTable.PrimaryKey, DBSetValueMode.Loading);
                }
                else if (log.LogType == DBLogType.Delete && !changed.ContainsKey(row))
                {
                    continue;
                }
                log.Upload(row);

                if (log.LogType == DBLogType.Insert)
                {
                    row.UpdateState |= DBUpdateState.Delete;
                }
                else if (log.LogType == DBLogType.Delete)
                {
                    row.UpdateState |= DBUpdateState.Insert;
                    log.BaseTable.Add(row);
                }
                else if (log.LogType == DBLogType.Update && row.GetIsChanged())
                {
                    row.UpdateState |= DBUpdateState.Update;
                }

                log.Status = DBStatus.Delete;

                if (!changed.TryGetValue(row, out var list))
                    changed[row] = list = new List<T>();

                list.Add(log);
            }

            foreach (var entry in changed)
            {
                using (var transaction = new DBTransaction(entry.Key.Table, user))
                {
                    //var currentLog = entry.Key.Table.LogTable.NewItem();
                    await entry.Key.Save(transaction);

                    foreach (var item in entry.Value)
                    {
                        await item.Save(transaction);
                    }
                    transaction.Commit();
                }
            }
        }

        public static async Task Accept(DBItem row, IEnumerable<T> logs, IUserIdentity user)
        {
            if (row.Status == DBStatus.Edit || row.Status == DBStatus.New || row.Status == DBStatus.Error)
                row.Status = DBStatus.Actual;
            else if (row.Status == DBStatus.Delete)
                row.Delete();
            using (var transaction = new DBTransaction(row.Table, user))
            {
                await row.Save(transaction);

                foreach (var item in logs)
                {
                    if (item.Status == DBStatus.New)
                    {
                        item.Status = DBStatus.Actual;
                        await item.Save(transaction);
                    }
                }
                transaction.Commit();
            }
        }

    }
}
