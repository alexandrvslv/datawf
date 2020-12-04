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

[assembly: Invoker(typeof(DBLogTable<>), nameof(DBLogTable<DBLogItem>.BaseTableName), typeof(DBLogTable<>.BaseTableNameInvoker))]
[assembly: Invoker(typeof(DBLogTable<>), nameof(DBLogTable<DBLogItem>.BaseKey), typeof(DBLogTable<>.BaseKeyInvoker))]
[assembly: Invoker(typeof(DBLogTable<>), nameof(DBLogTable<DBLogItem>.UserLogKey), typeof(DBLogTable<>.UserLogKeyInvoker))]
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
                

                foreach (var column in value.Columns)
                {
                    if (column.ColumnType == DBColumnTypes.Default
                        && (column.Keys & DBColumnKeys.NoLog) != DBColumnKeys.NoLog)
                    {
                        var logColumn = GetLogColumn(column);
                        if (logColumn == null)
                        {
                            logColumn = DBColumnFactory.CreateLog(column);
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
                && item is DBLogItem logItem && FileBLOBKey != null)
            {
                var lob = item.GetValue(FileBLOBKey);
                var current = logItem.BaseItem == DBItem.EmptyItem ? null : logItem.BaseItem.GetValue((DBColumn<long?>)FileBLOBKey.BaseColumn);
                if (lob != null && lob != current)
                {
                    var qquery = new QQuery(this);
                    qquery.BuildParam(FileBLOBKey, lob);
                    if (!Load(qquery).Any(p => p != item))
                    {
                        try
                        {
                            using (var transactionDeleteLOB = new DBTransaction(transaction.DbConnection, transaction.Caller))
                            {
                                await System.DeleteBLOB(lob.Value, transactionDeleteLOB);
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

        public class BaseTableNameInvoker : Invoker<DBLogTable<T>, string>
        {
            public override string Name => nameof(DBLogTable<T>.BaseTableName);

            public override bool CanWrite => true;

            public override string GetValue(DBLogTable<T> target) => target.BaseTableName;

            public override void SetValue(DBLogTable<T> target, string value) => target.BaseTableName = value;
        }

        public class BaseKeyInvoker : Invoker<DBLogTable<T>, DBColumn>
        {
            public override string Name => nameof(DBLogTable<T>.BaseKey);

            public override bool CanWrite => false;

            public override DBColumn GetValue(DBLogTable<T> target) => target.BaseKey;

            public override void SetValue(DBLogTable<T> target, DBColumn value) { }
        }

        public class UserLogKeyInvoker : Invoker<DBLogTable<T>, DBColumn>
        {
            public override string Name => nameof(DBLogTable<T>.UserLogKey);

            public override bool CanWrite => false;

            public override DBColumn GetValue(DBLogTable<T> target) => target.UserLogKey;

            public override void SetValue(DBLogTable<T> target, DBColumn value) { }
        }


    }
}
