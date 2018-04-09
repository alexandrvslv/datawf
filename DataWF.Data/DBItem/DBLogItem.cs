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
using System.ComponentModel;
using DataWF.Common;

namespace DataWF.Data
{
    public class DBLogItem : DBItem
    {
        private DBItem baseItem;

        public DBLogItem()
        { }

        public DBLogItem(DBItem item)
        {
            BaseItem = item;
        }

        public int? LogId
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        public DBLogType? LogType
        {
            get { return GetValue<DBLogType?>(Table.ElementTypeKey); }
            set { SetValue(value, Table.ElementTypeKey); }
        }

        public object BaseId
        {
            get { return GetValue(LogTable.BaseKey); }
        }

        public DBItem BaseItem
        {
            get { return baseItem ?? (baseItem = BaseTable.LoadItemById(BaseId)); }
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

        public void Upload()
        {
            Upload(BaseItem);
        }

        public void Upload(DBItem value)
        {
            foreach (var logColumn in LogTable.GetLogColumns())
            {
                value.SetValue(GetValue(logColumn), logColumn.BaseColumn);
            }
        }

        public DBLogItem GetPrevius()
        {
            var query = new QQuery("", LogTable);
            query.Columns.Add(new QFunc(QFunctionType.max)
            {
                Items = new QItemList<QItem>(new[] { new QColumn(LogTable.PrimaryKey) })
            });
            query.BuildParam(LogTable.PrimaryKey, CompareType.Less, LogId);
            query.BuildParam(LogTable.BaseKey, CompareType.Equal, BaseId);
            query.Orders.Add(new QOrder(LogTable.PrimaryKey));

            var id = LogTable.Schema.Connection.ExecuteQuery(query.ToWhere());
            return LogTable.LoadById(id);
        }

        public override string ToString()
        {
            return $"{LogType} {BaseItem}";
        }
    }
}
