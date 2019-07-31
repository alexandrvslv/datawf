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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public class DBLogItem : DBItem
    {
        private DBItem baseItem = DBItem.EmptyItem;

        public DBLogItem()
        { }

        public DBLogItem(DBItem item)
        {
            BaseItem = item;
        }

        public long? LogId
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        public DBLogType? LogType
        {
            get { return GetValue<DBLogType?>(Table.ElementTypeKey); }
            set { SetValue(value, Table.ElementTypeKey); }
        }

        public long? UserLogId
        {
            get { return GetValue<long?>(LogTable.UserLogKey); }
            set { SetValue(value, LogTable.UserLogKey); }
        }

        public DBItem UserLog
        {
            get { return DBLogTable.UserLogTable?.LoadItemById(UserLogId); }
            set { UserLogId = (long?)value?.PrimaryId; }
        }

        public object BaseId
        {
            get { return GetValue(LogTable.BaseKey); }
        }

        public DBItem BaseItem
        {
            get { return baseItem == DBItem.EmptyItem ? (baseItem = BaseTable.LoadItemById(BaseId)) : baseItem; }
            set
            {
                baseItem = value;
                Build(value.Table.LogTable);
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

        public DBTable BaseTable { get { return LogTable?.BaseTable; } }

        [Browsable(false)]
        public DBLogTable LogTable { get { return (DBLogTable)Table; } }

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
            using (var query = new QQuery("", LogTable))
            {
                query.Columns.Add(new QFunc(QFunctionType.max)
                {
                    Items = new QItemList<QItem>(new[] { new QColumn(LogTable.PrimaryKey) })
                });
                query.BuildParam(LogTable.PrimaryKey, CompareType.Less, LogId);
                query.BuildParam(LogTable.BaseKey, CompareType.Equal, BaseId);
                //query.Orders.Add(new QOrder(LogTable.PrimaryKey));

                var id = transaction.ExecuteQuery(query.Format());
                return LogTable.LoadById(id, DBLoadParam.Load, null, transaction);
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
