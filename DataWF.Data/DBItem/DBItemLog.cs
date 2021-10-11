﻿//  The MIT License (MIT)
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
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public partial class DBItemLog : DBItem
    {
        public static DBTable UserRegTable { get; set; }
        public static readonly string UserLogKeyName = "userlog_id";

        private DBItem baseItem;
        private DBUserReg dbUserReg;

        public DBItemLog(IDBSchema schema) : base(schema)
        { }

        public DBItemLog(IDBTableLog table) : base(table)
        { }

        public DBItemLog(DBItem item) : this(item.Table.LogTable)
        {
            BaseItem = item;
        }

        [XmlIgnore, JsonIgnore]
        public new IDBTableLog Table
        {
            get => (IDBTableLog)base.Table;
            set => base.Table = value;
        }

        [Column("logid", Keys = DBColumnKeys.Primary)]
        public long LogId
        {
            get => GetValue(Table.LogIdKey);
            set => SetValue(value, Table.LogIdKey);
        }

        [Column("logtype", Keys = DBColumnKeys.ElementType)]
        public DBLogType? LogType
        {
            get => GetValue(Table.LogTypeKey);
            set => SetValue(value, Table.LogTypeKey);
        }

        [Column("userlog_id")]
        public long? UserRegId
        {
            get => GetValue(Table.UserLogKey);
            set => SetValue(value, Table.UserLogKey);
        }

        [Reference(nameof(UserRegId))]
        public DBUserReg UserReg
        {
            get => GetReference(Table.UserLogKey, ref dbUserReg);
            set => SetReference(dbUserReg = value, Table.UserLogKey);
        }

        [LogColumn("item_type", "item_type_log", GroupName = "system", Keys = DBColumnKeys.ItemType, Order = 0), DefaultValue(0)]
        public override int ItemType
        {
            get => base.ItemType;
            set => base.ItemType = value;
        }

        [Column("datecreate", GroupName = "system", Keys = DBColumnKeys.Date | DBColumnKeys.System | DBColumnKeys.UtcDate, Order = 100)]
        public override DateTime DateCreate
        {
            get => base.DateCreate;
            set => base.DateCreate = value;
        }

        [XmlIgnore, JsonIgnore, NotMapped, Browsable(false)]
        [LogColumn("group_access", "group_access_log", Size = 512, DataType = typeof(byte[]), GroupName = "system", Keys = DBColumnKeys.Access | DBColumnKeys.System, Order = 102)]
        public override AccessValue Access
        {
            get => base.Access;
            set => base.Access = value;
        }

        [Column("base_id", ColumnType = DBColumnTypes.Code, Keys = DBColumnKeys.System)]
        public string BaseId
        {
            get => GetValue(Table.BaseKey)?.ToString();
            set { }
        }

        [XmlIgnore, JsonIgnore]
        public DBItem BaseItem
        {
            get => baseItem ??= (BaseTable.PrimaryKey.Load(GetValue(Table.BaseKey)).FirstOrDefault() ?? DBItem.EmptyItem);
            set
            {
                baseItem = value;
                Build((DBTable)value.Table.LogTable);
                LogType = value.UpdateState.HasFlag(DBUpdateState.Insert)
                              ? DBLogType.Insert : value.UpdateState.HasFlag(DBUpdateState.Update)
                              ? DBLogType.Update : value.UpdateState.HasFlag(DBUpdateState.Delete)
                              ? DBLogType.Delete : DBLogType.None;
                foreach (var column in Table.GetLogColumns())
                {
                    column.Copy(value, column.TargetColumn, this);
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public IDBTable BaseTable => Table?.TargetTable;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public IDBTableLog LogTable => (IDBTableLog)Table;

        public virtual DBUser GetUser()
        {
            var reg = UserRegTable?.LoadById<DBUserReg>(UserRegId);
            return reg?.User;
        }

        public async Task<DBItem> Redo(DBTransaction transaction)
        {
            if (BaseItem == DBItem.EmptyItem)
            {
                baseItem = BaseTable.NewItem(DBUpdateState.Insert, false, (int)GetValue<int?>(BaseTable.ItemTypeKey.LogColumn));
            }
            else if (BaseItem.UpdateState == DBUpdateState.Delete)
            {
                BaseItem.UpdateState = DBUpdateState.Insert;
            }
            Upload(BaseItem);
            BaseItem.Attach();
            await UndoReferences(transaction);
            await RedoFile(transaction);
            if (BaseItem.IsChanged)
            {
                await BaseTable.Save(BaseItem, transaction);
            }
            if (baseItem != null && LogType == DBLogType.Delete)
            {
                await UndoReferencing(transaction, baseItem);
            }

            return baseItem;
        }

        private async Task RedoFile(DBTransaction transaction)
        {
            if (Table.FileKey != null
                  && BaseTable.FileOIDKey != null
                  && BaseItem.GetValue(BaseTable.FileOIDKey) == null)
            {
                try
                {
                    using (var stream = GetMemoryStream(Table.FileKey, transaction))
                    {
                        await BaseItem.SetBlob(stream, BaseTable.FileOIDKey, transaction);
                    }
                }
                catch { }
            }
        }

        public void Upload(DBItem value)
        {
            foreach (var logColumn in Table.GetLogColumns())
            {
                logColumn.TargetColumn.Copy(this, logColumn, value);
            }
        }

        public DBItemLog GetPrevius(IUserIdentity user = null)
        {
            using (var transaction = new DBTransaction(Table, user, true))
            {
                return GetPrevius(transaction);
            }
        }

        public DBItemLog GetPrevius(DBTransaction transaction)
        {
            var query = LogTable.QQuery()
                .Column(QFunctionType.max, LogTable.PrimaryKey)
                .Where(LogTable.PrimaryKey, CompareType.Less, LogId)
                .And(LogTable.BaseKey, CompareType.Equal, GetValue(LogTable.BaseKey));
            //from sss in indexer 
            //query.Orders.Add(new QOrder(LogTable.PrimaryKey));

            var id = transaction.ExecuteQuery(query.Format());
            return LogTable.LoadById<DBItemLog>(id, DBLoadParam.Load, transaction);
        }

        public override Task SaveReferenced(DBTransaction transaction)
        {
            return Task.CompletedTask;
        }

        public override void Reject(IUserIdentity user)
        {
            base.Reject(user);
            //Clear();
        }

        public override void Accept(IUserIdentity user)
        {
            base.Accept(user);
            //Clear();
        }

        public override string ToString()
        {
            return $"{LogType} {BaseItem}";
        }

        public async Task<DBItem> Undo(DBTransaction transaction)
        {
            var baseItem = await Redo(transaction);

            return baseItem;
        }

        private async Task UndoReferencing(DBTransaction transaction, DBItem baseItem)
        {
            foreach (var reference in BaseTable.GetReferencingByProperty(baseItem.GetType()))
            {
                var referenceTable = reference.ReferenceTable;
                var referenceColumn = reference.ReferenceColumn;

                if (referenceTable?.LogTable == null || referenceColumn?.LogColumn == null)
                    continue;
                var stack = new HashSet<object>();
                var query = referenceTable.LogTable.QQuery()
                    .Where(referenceColumn.LogColumn, CompareType.Equal, BaseId)
                    .And(referenceTable.LogTable.ElementTypeKey, CompareType.Equal, DBLogType.Delete)
                    .OrderBy(referenceTable.LogTable.DateCreateKey);

                var logItems = referenceTable.LogTable.Load<DBItemLog>(query).OrderByDescending(p => p.DateCreate);
                foreach (var refed in logItems)
                {
                    if (!stack.Contains(refed.BaseId) && Math.Abs((DateCreate - refed.DateCreate).TotalMinutes) < 5)
                    {
                        stack.Add(refed.BaseId);
                        await refed.Undo(transaction);
                    }
                }
            }
        }

        private async Task UndoReferences(DBTransaction transaction)
        {
            foreach (var column in BaseTable.Columns.GetIsReference())
            {
                if (column.ReferenceTable == null
                    || !TypeHelper.IsBaseType(BaseItem.GetType(), column.PropertyInvoker?.TargetType))
                    continue;
                if (!column.IsEmpty(BaseItem) && BaseItem.GetReference(column) == null)
                {
                    if (column.ReferenceTable.IsLoging)
                    {
                        var query = column.ReferenceTable.LogTable.QQuery()
                            .Where(column.ReferenceTable.LogTable.BaseKey, CompareType.Equal, BaseItem.GetValue(column))
                            .And(column.ReferenceTable.LogTable.ElementTypeKey, CompareType.Equal, DBLogType.Delete);
                        var logItem = column.ReferenceTable.LogTable.Load<DBItemLog>(query).OrderByDescending(p => p.DateCreate).FirstOrDefault();
                        if (logItem != null)
                        {
                            await logItem.Undo(transaction);
                        }
                        else
                        {
                            BaseItem.SetValue(null, column);
                        }
                    }
                    else
                    {
                        BaseItem.SetValue(null, column);
                    }
                }
            }
        }


    }
}