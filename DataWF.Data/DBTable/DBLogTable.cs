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
    public partial class DBLogTable<T> : DBTable<T>, IDBLogTable where T : DBLogItem
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

        [XmlIgnore, JsonIgnore]
        public override DBTable BaseTable
        {
            get
            {
                return baseTable ??= (Schema is DBLogSchema logSchema
                              ? logSchema.BaseSchema.Tables[BaseTableName]
                              : Schema?.Tables[BaseTableName]);
            }
            set
            {
                BaseTableName = value?.Name ?? throw new ArgumentException("BaseTable set operation required not null value!");
                baseTable = value;
                Name = value.Name + "_log";
                Schema = value.Schema.LogSchema ?? value.Schema;
                var seqName = value.SequenceName + "_log";
                Sequence = Schema.Sequences[seqName] ?? new DBSequence() { Name = seqName };
                DisplayName = value.DisplayName + " Log";


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

        public DBColumn GetLogColumn(DBColumn column)
        {
            return ParseColumn(column.Name + "_log");
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

        public IEnumerable<DBColumn> GetLogColumns()
        {
            foreach (var column in Columns)
            {
                if (column.BaseColumn != null)
                {
                    yield return column;
                }
            }
        }

        public override async Task<bool> SaveItem(DBItem item, DBTransaction transaction)
        {
            if ((item.UpdateState & DBUpdateState.Delete) == DBUpdateState.Delete
                && item is DBLogItem logItem && FileOIDKey != null
                && !transaction.Replication)
            {
                var lob = item.GetValue(FileOIDKey);
                var current = logItem.BaseItem == DBItem.EmptyItem ? null : logItem.BaseItem.GetValue((DBColumn<long?>)FileOIDKey.BaseColumn);
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
                if (column.BaseName != null && column.BaseColumn == null)
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

    }
}
