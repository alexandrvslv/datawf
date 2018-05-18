/*
 DocumentLog.cs

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
using System.Collections.Generic;
using DataWF.Data;
using DataWF.Common;

namespace DataWF.Module.Common
{
    public class ItemDataLog
    {
        private DBItem row;
        private List<DBLogItem> logs;
        private SelectableList<LogChange> changes = new SelectableList<LogChange>();
        private string user;
        private DBTable table;
        private string text;

        public ItemDataLog()
        { }

        public ItemDataLog(DBItem row)
        {
            changes.Indexes.Add(new Invoker<LogChange, DBColumn>(nameof(LogChange.Column),
                        (item) => item.Column,
                        (item, value) => item.Column = value));
            Row = row;
        }

        public string User
        {
            get { return user; }
        }

        public DBItem Row
        {
            get { return row; }
            set
            {
                row = value;
                if (row != null)
                    table = row.Table;
                RefreshLogs();
            }
        }

        public void RefreshLogs()
        {
            QQuery query = new QQuery(string.Empty, Row.Table.LogTable);
            query.BuildParam(Row.Table.LogTable.BaseKey, CompareType.Equal, row.PrimaryId);
            query.BuildParam(Row.Table.LogTable.StatusKey, CompareType.Equal, DBStatus.New);
            Logs.AddRange(Row.Table.LogTable.Load(query, DBLoadParam.Load | DBLoadParam.Synchronize));
        }

        public List<DBLogItem> Logs
        {
            get
            {
                if (logs == null)
                    logs = new List<DBLogItem>();//DataLog.DBTable);
                return logs;
            }
            set
            {
                logs = value;
                RefreshChanges();
            }
        }
        public DBTable Table
        {
            get { return table; }
            set { table = value; }
        }

        public void RefreshChanges()
        {
            logs.Sort(new DBComparer(Row.Table.LogTable.PrimaryKey));

            changes.Clear();
            user = string.Empty;
            DBLogItem prev = null;
            foreach (var log in Logs)
            {
                if (log.Status == DBStatus.New)
                {
                    string name = ((UserLog)log.UserLog)?.User?.Name;
                    if (user.IndexOf(name, StringComparison.Ordinal) < 0)
                        user += name + "; ";
                    foreach (var logColumn in log.LogTable.GetLogColumns())
                    {
                        LogChange map = changes.SelectOne(nameof(LogChange.Column), CompareType.Equal, logColumn.BaseColumn);
                        if (map == null)
                        {
                            map = new LogChange();
                            map.Column = logColumn.BaseColumn;
                            map.Old = prev?.GetValue(logColumn);
                            changes.Add(map);
                        }
                        if (!map.User.Contains(name))
                            map.User += name + "; ";
                        map.New = log.GetValue(logColumn);
                    }
                    prev = log;
                }
            }
        }

        public string Text { get { return text; } set { text = value; } }

        public SelectableList<LogChange> Changes
        {
            get { return changes; }
        }

        public bool Check()
        {
            bool flag = true;
            foreach (var log in Logs)
            {
                if (log.Status == DBStatus.New && (((UserLog)log.UserLog)?.User?.IsCurrent ?? false))// && !log.User.Super
                {
                    flag = false;
                    break;
                }
            }
            return flag;
        }

        public void Accept()
        {
            UserLog.Accept(row, logs);
            RefreshLogs();
        }

        public void Reject()
        {
            UserLog.Reject(logs);
            RefreshLogs();
        }


        //public List<UserLog> GetChilds()
        //{
        //    var query = new QQuery("", UserLog.DBTable);
        //    query.BuildPropertyParam(nameof(UserLog.ParentId), CompareType.In, logs);
        //    return UserLog.DBTable.Load(query);
        //}
    }
}
