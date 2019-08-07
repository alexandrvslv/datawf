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

        private DBItem baseItem = DBItem.EmptyItem;
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

        [Column("item_type_log", GroupName = "system", Keys = DBColumnKeys.ItemType, Order = 0)]
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
        [Column("group_access_log", 512, DataType = typeof(byte[]), GroupName = "system", Keys = DBColumnKeys.Access | DBColumnKeys.System, Order = 102)]
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
            get { return baseItem == DBItem.EmptyItem ? (baseItem = BaseTable.LoadItemById(GetValue(LogTable.BaseKey))) : baseItem; }
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
            if (BaseItem == null)
                baseItem = BaseTable.NewItem(DBUpdateState.Insert, false, (int)GetValue<int?>(BaseTable.ItemTypeKey.LogColumn));
            Upload(BaseItem);
            await BaseItem.Save(transaction);
            return baseItem;
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
                transaction.NoLogs = true;
                baseItem = await item.Redo(transaction);
                transaction.NoLogs = false;
                if (baseItem != null && LogType == DBLogType.Delete)
                {
                    foreach (var tableReference in BaseTable.GetChildRelations()
                        .Where(p => !(p.Table is IDBVirtualTable)
                                  && p.Table.IsLoging
                                  && p.Column.LogColumn != null))
                    {
                        using (var query = new QQuery((DBTable)tableReference.Table.LogTable))
                        {
                            query.BuildParam(tableReference.Column.LogColumn, CompareType.Equal, item.BaseId);
                            query.BuildParam(tableReference.Table.LogTable.ElementTypeKey, CompareType.Equal, DBLogType.Delete);
                            foreach (var refed in tableReference.Table.LogTable.LoadItems(query).Cast<DBLogItem>().ToList())
                                await refed.Undo(transaction);
                        }

                    }
                }
            }
            await Delete(logtransaction);
            return baseItem;
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
    }
}
