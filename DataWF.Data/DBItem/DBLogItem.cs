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
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

[assembly: Invoker(typeof(DBLogItem), nameof(DBLogItem.LogId), typeof(DBLogItem.LogIdInvoker<>))]
[assembly: Invoker(typeof(DBLogItem), nameof(DBLogItem.LogType), typeof(DBLogItem.LogTypeInvoker<>))]
[assembly: Invoker(typeof(DBLogItem), nameof(DBLogItem.UserRegId), typeof(DBLogItem.UserRegIdInvoker<>))]
[assembly: Invoker(typeof(DBLogItem), nameof(DBLogItem.BaseId), typeof(DBLogItem.BaseIdInvoker<>))]
[assembly: Invoker(typeof(DBLogItem), nameof(DBLogItem.BaseItem), typeof(DBLogItem.BaseItemInvoker<>))]
namespace DataWF.Data
{
    public class DBLogItem : DBItem
    {
        public static DBTable UserLogTable { get; set; }
        public static readonly string UserLogKeyName = "userlog_id";

        private DBItem baseItem;

        public DBLogItem()
        { }

        public DBLogItem(DBItem item)
        {
            BaseItem = item;
        }

        [Column("logid", Keys = DBColumnKeys.Primary)]
        public long LogId
        {
            get => GetValue<long>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("logtype", Keys = DBColumnKeys.ElementType)]
        public DBLogType? LogType
        {
            get => GetValue<DBLogType?>(Table.ElementTypeKey);
            set => SetValue(value, Table.ElementTypeKey);
        }

        [Column("userlog_id")]
        public long? UserRegId
        {
            get => GetValue<long?>(LogTable.UserLogKey);
            set => SetValue(value, LogTable.UserLogKey);
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
            get => GetValue(LogTable.BaseKey)?.ToString();
            set { }
        }

        [XmlIgnore, JsonIgnore]
        public DBItem BaseItem
        {
            get => baseItem ?? (baseItem = BaseTable.PrimaryKey.LoadByKey(GetValue(LogTable.BaseKey)) ?? DBItem.EmptyItem);
            set
            {
                baseItem = value;
                Build((DBTable)value.Table.LogTable);
                LogType = value.UpdateState.HasFlag(DBUpdateState.Insert)
                              ? DBLogType.Insert : value.UpdateState.HasFlag(DBUpdateState.Update)
                              ? DBLogType.Update : value.UpdateState.HasFlag(DBUpdateState.Delete)
                              ? DBLogType.Delete : DBLogType.None;
                foreach (var column in LogTable.GetLogColumns())
                {
                    column.Copy(value, column.BaseColumn, this);
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public DBTable BaseTable => LogTable?.BaseTable;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public IDBLogTable LogTable => (IDBLogTable)Table;

        public virtual DBUser GetUser()
        {
            var reg = (DBUserReg)UserLogTable?.LoadItemById(UserRegId);
            return reg?.DBUser;
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
                await BaseTable.SaveItem(BaseItem, transaction);
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
                  && BaseTable.FileBLOBKey != null
                  && BaseItem.GetValue(BaseTable.FileBLOBKey) == null)
            {
                try
                {
                    using (var stream = GetMemoryStream(Table.FileKey, transaction))
                    {
                        await BaseItem.SetBLOB(stream, BaseTable.FileBLOBKey, transaction);
                    }
                }
                catch { }
            }
        }

        public void Upload(DBItem value)
        {
            foreach (var logColumn in LogTable.GetLogColumns())
            {
                logColumn.BaseColumn.Copy(this, logColumn, value);
            }
        }

        public DBLogItem GetPrevius(IUserIdentity user = null)
        {
            using (var transaction = new DBTransaction(Table.Connection, user, true))
            {
                return GetPrevius(transaction);
            }
        }

        public DBLogItem GetPrevius(DBTransaction transaction)
        {
            using (var query = new QQuery("", (DBTable)LogTable))
            {
                query.Columns.Add(new QFunc(QFunctionType.max, new[] { new QColumn(LogTable.PrimaryKey) }));
                query.BuildParam(LogTable.PrimaryKey, CompareType.Less, LogId);
                query.BuildParam(LogTable.BaseKey, CompareType.Equal, GetValue(LogTable.BaseKey));
                //query.Orders.Add(new QOrder(LogTable.PrimaryKey));

                var id = transaction.ExecuteQuery(query.Format());
                return (DBLogItem)LogTable.LoadItemById(id, DBLoadParam.Load, null, transaction);
            }
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
            foreach (var reference in BaseTable.GetPropertyReferencing(baseItem.GetType()))
            {
                var referenceTable = reference.ReferenceTable?.Table;
                var referenceColumn = reference.ReferenceColumn?.Column;

                if (referenceTable?.LogTable == null || referenceColumn?.LogColumn == null)
                    continue;
                var stack = new HashSet<object>();
                using (var query = new QQuery((DBTable)referenceTable.LogTable))
                {
                    query.BuildParam(referenceColumn.LogColumn, CompareType.Equal, BaseId);
                    query.BuildParam(referenceTable.LogTable.ElementTypeKey, CompareType.Equal, DBLogType.Delete);
                    var logItems = referenceTable.LogTable.LoadItems(query).Cast<DBLogItem>().OrderByDescending(p => p.DateCreate);
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
                        using (var query = new QQuery((DBTable)column.ReferenceTable.LogTable))
                        {
                            query.BuildParam(column.ReferenceTable.LogTable.BaseKey, CompareType.Equal, BaseItem.GetValue(column));
                            query.BuildParam(column.ReferenceTable.LogTable.ElementTypeKey, CompareType.Equal, DBLogType.Delete);
                            var logItem = column.ReferenceTable.LogTable.LoadItems(query).Cast<DBLogItem>().OrderByDescending(p => p.DateCreate).FirstOrDefault();
                            if (logItem != null)
                            {
                                await logItem.Undo(transaction);
                            }
                            else
                            {
                                BaseItem.SetValue(null, column);
                            }
                        }
                    }
                    else
                    {
                        BaseItem.SetValue(null, column);
                    }
                }
            }
        }

        public static async Task Reject(IEnumerable<DBLogItem> redo, IUserIdentity user)
        {
            var changed = new Dictionary<DBItem, List<DBLogItem>>();
            foreach (DBLogItem log in redo.OrderBy(p => p.PrimaryId))
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
                    changed[row] = list = new List<DBLogItem>();

                list.Add(log);
            }

            foreach (var entry in changed)
            {
                using (var transaction = new DBTransaction(entry.Key.Table.Connection, user))
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

        public static async Task Accept(DBItem row, IEnumerable<DBLogItem> logs, IUserIdentity user)
        {
            if (row.Status == DBStatus.Edit || row.Status == DBStatus.New || row.Status == DBStatus.Error)
                row.Status = DBStatus.Actual;
            else if (row.Status == DBStatus.Delete)
                row.Delete();
            using (var transaction = new DBTransaction(row.Table.Connection, user))
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

        public class LogIdInvoker<T> : Invoker<T, long> where T : DBLogItem
        {
            public override string Name => nameof(DBLogItem.LogId);

            public override bool CanWrite => true;

            public override long GetValue(T target) => target.LogId;

            public override void SetValue(T target, long value) => target.LogId = value;
        }

        public class LogTypeInvoker<T> : Invoker<T, DBLogType?> where T : DBLogItem
        {
            public override string Name => nameof(DBLogItem.LogType);

            public override bool CanWrite => true;

            public override DBLogType? GetValue(T target) => target.LogType;

            public override void SetValue(T target, DBLogType? value) => target.LogType = value;
        }

        public class UserRegIdInvoker<T> : Invoker<T, long?> where T : DBLogItem
        {
            public override string Name => nameof(DBLogItem.UserRegId);

            public override bool CanWrite => true;

            public override long? GetValue(T target) => target.UserRegId;

            public override void SetValue(T target, long? value) => target.UserRegId = value;
        }

        public class BaseIdInvoker<T> : Invoker<T, string> where T : DBLogItem
        {
            public override string Name => nameof(DBLogItem.BaseId);

            public override bool CanWrite => false;

            public override string GetValue(T target) => target.BaseId;

            public override void SetValue(T target, string value) { }
        }

        public class BaseItemInvoker<T> : Invoker<T, DBItem> where T : DBLogItem
        {
            public override string Name => nameof(DBLogItem.BaseItem);

            public override bool CanWrite => false;

            public override DBItem GetValue(T target) => target.BaseItem;

            public override void SetValue(T target, DBItem value) => target.BaseItem = value;
        }
    }
}
