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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBLogItem : DBItem
    {
        public static DBTable UserLogTable { get; set; }
        public static readonly string UserLogKeyName = "userlog_id";

        private DBItem baseItem;
        private DBUserReg userLog;

        public DBLogItem()
        { }

        public DBLogItem(DBItem item)
        {
            BaseItem = item;
        }

        [Column("logid", Keys = DBColumnKeys.Primary)]
        public long? LogId
        {
            get => GetValue<long?>(Table.PrimaryKey);
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

        public DBUserReg UserReg
        {
            get => userLog ?? (userLog = (DBUserReg)UserLogTable?.LoadItemById(UserRegId));
            set => UserRegId = (long?)(userLog = value)?.PrimaryId;
        }

        [Column("loguser_id", ColumnType = DBColumnTypes.Code)]
        public int? LogUserId
        {
            get => UserReg?.UserId;
        }

        [LogColumn("item_type", "item_type_log", GroupName = "system", Keys = DBColumnKeys.ItemType, Order = 0), DefaultValue(0)]
        public override int? ItemType
        {
            get => base.ItemType;
            set => base.ItemType = value;
        }

        [Column("datecreate", GroupName = "system", Keys = DBColumnKeys.Date | DBColumnKeys.System, Order = 100)]
        public override DateTime? DateCreate
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

        [Column("base_id", ColumnType = DBColumnTypes.Code)]
        public string BaseId
        {
            get => GetValue(LogTable.BaseKey)?.ToString();
        }

        [XmlIgnore, JsonIgnore]
        public DBItem BaseItem
        {
            get { return baseItem ?? (baseItem = BaseTable.LoadItemById(GetValue(LogTable.BaseKey)) ?? DBItem.EmptyItem); }
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
                    SetValue(value.GetValue(column.BaseColumn), column);
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public DBTable BaseTable => LogTable?.BaseTable;

        [Browsable(false)]
        public IDBLogTable LogTable => (IDBLogTable)Table;

        public async Task<DBItem> Redo(DBTransaction transaction)
        {
            if (BaseItem == DBItem.EmptyItem)
            {
                baseItem = BaseTable.NewItem(DBUpdateState.Insert, false, (int)GetValue<int?>(BaseTable.ItemTypeKey.LogColumn));
            }

            Upload(BaseItem);
            BaseItem.Attach();
            await UploadReferences(transaction);
            await RedoFile(transaction);
            if (BaseItem.IsChanged)
            {
                await BaseTable.SaveItem(BaseItem, transaction);
            }

            return baseItem;
        }

        private async Task RedoFile(DBTransaction transaction)
        {
            if (Table.FileKey != null
                  && BaseTable.FileLOBKey != null
                  && BaseItem.GetValue(BaseTable.FileLOBKey) == null)
            {
                try
                {
                    using (var stream = GetMemoryStream(Table.FileKey, transaction))
                    {
                        await BaseItem.SetLOB(stream, BaseTable.FileLOBKey, transaction);
                    }
                }
                catch { }
            }
        }

        public void Upload(DBItem value)
        {
            foreach (var logColumn in LogTable.GetLogColumns())
            {
                value.SetValue(GetValue(logColumn), logColumn.BaseColumn);
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
                query.Columns.Add(new QFunc(QFunctionType.max)
                {
                    Items = new QItemList<QItem>(new[] { new QColumn(LogTable.PrimaryKey) })
                });
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
            var logtransaction = transaction.GetSubTransaction(Table.Connection);
            var item = GetPrevius(logtransaction);
            var baseItem = BaseItem;
            if (item != null)
            {
                baseItem = await item.Redo(transaction);

                if (baseItem != null && LogType == DBLogType.Delete)
                {
                    await UndoReferencing(transaction);
                }
            }
            return baseItem;
        }

        private async Task UndoReferencing(DBTransaction transaction)
        {
            foreach (var tableReference in BaseTable.GetChildRelations()
                                    .Where(p => !(p.Table is IDBVirtualTable)
                                              && p.Table.IsLoging
                                              && p.Column.LogColumn != null))
            {
                using (var query = new QQuery((DBTable)tableReference.Table.LogTable))
                {
                    query.BuildParam(tableReference.Column.LogColumn, CompareType.Equal, BaseId);
                    query.BuildParam(tableReference.Table.LogTable.ElementTypeKey, CompareType.Equal, DBLogType.Delete);
                    var logItems = tableReference.Table.LogTable.LoadItems(query).Cast<DBLogItem>().ToList();
                    foreach (var refed in logItems)
                        await refed.Undo(transaction);
                }

            }
        }

        private async Task UploadReferences(DBTransaction transaction)
        {
            foreach (var column in BaseTable.Columns.GetIsReference())
            {
                var value = BaseItem.GetValue(column);
                if (value != null && BaseItem.GetReference(column) == null)
                {
                    if (column.ReferenceTable.IsLoging)
                    {
                        using (var query = new QQuery((DBTable)column.ReferenceTable.LogTable))
                        {
                            query.BuildParam(column.ReferenceTable.LogTable.BaseKey, CompareType.Equal, value);
                            query.BuildParam(column.ReferenceTable.LogTable.ElementTypeKey, CompareType.Equal, DBLogType.Delete);
                            var logItem = column.ReferenceTable.LogTable.LoadItems(query).Cast<DBLogItem>().FirstOrDefault();
                            if (logItem != null)
                            {
                                await logItem.Undo(transaction);
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
                    row.SetValue(log.BaseId, log.BaseTable.PrimaryKey, false);
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

        [Invoker(typeof(DBLogItem), nameof(DBLogItem.LogId))]
        public class LogIdInvoker<T> : Invoker<T, long?> where T : DBLogItem
        {
            public override string Name => nameof(DBLogItem.LogId);

            public override bool CanWrite => true;

            public override long? GetValue(T target) => target.LogId;

            public override void SetValue(T target, long? value) => target.LogId = value;
        }

        [Invoker(typeof(DBLogItem), nameof(DBLogItem.LogType))]
        public class LogTypeInvoker<T> : Invoker<T, DBLogType?> where T : DBLogItem
        {
            public override string Name => nameof(DBLogItem.LogType);

            public override bool CanWrite => true;

            public override DBLogType? GetValue(T target) => target.LogType;

            public override void SetValue(T target, DBLogType? value) => target.LogType = value;
        }

        [Invoker(typeof(DBLogItem), nameof(DBLogItem.UserRegId))]
        public class UserRegIdInvoker<T> : Invoker<T, long?> where T : DBLogItem
        {
            public override string Name => nameof(DBLogItem.UserRegId);

            public override bool CanWrite => true;

            public override long? GetValue(T target) => target.UserRegId;

            public override void SetValue(T target, long? value) => target.UserRegId = value;
        }

        [Invoker(typeof(DBLogItem), nameof(DBLogItem.UserReg))]
        public class UserRegInvoker<T> : Invoker<T, DBUserReg> where T : DBLogItem
        {
            public override string Name => nameof(DBLogItem.UserReg);

            public override bool CanWrite => true;

            public override DBUserReg GetValue(T target) => target.UserReg;

            public override void SetValue(T target, DBUserReg value) => target.UserReg = value;
        }

        [Invoker(typeof(DBLogItem), nameof(DBLogItem.LogUserId))]
        public class LogUserIdInvoker<T> : Invoker<T, int?> where T : DBLogItem
        {
            public override string Name => nameof(DBLogItem.LogUserId);

            public override bool CanWrite => false;

            public override int? GetValue(T target) => target.LogUserId;

            public override void SetValue(T target, int? value) { }
        }

        [Invoker(typeof(DBLogItem), nameof(DBLogItem.BaseId))]
        public class BaseIdInvoker<T> : Invoker<T, string> where T : DBLogItem
        {
            public override string Name => nameof(DBLogItem.BaseId);

            public override bool CanWrite => false;

            public override string GetValue(T target) => target.BaseId;

            public override void SetValue(T target, string value) { }
        }

        [Invoker(typeof(DBLogItem), nameof(DBLogItem.BaseItem))]
        public class BaseItemInvoker<T> : Invoker<T, DBItem> where T : DBLogItem
        {
            public override string Name => nameof(DBLogItem.BaseItem);

            public override bool CanWrite => false;

            public override DBItem GetValue(T target) => target.BaseItem;

            public override void SetValue(T target, DBItem value) => target.BaseItem = value;
        }
    }
}
