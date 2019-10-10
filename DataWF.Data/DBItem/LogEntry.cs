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
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public class LogEntry
    {
        //bool check
        public LogEntry()
        {
        }

        public string User { get; set; }

        public DBColumn Column { get; set; }

        public object Old { get; set; }

        public object New { get; set; }

        public object OldFormat
        {
            get { return Column.FormatValue(Old); }
            set { Old = value; }
        }

        public object NewFormat
        {
            get { return Column.FormatValue(New); }
            set { New = value; }
        }

        public object Tag { get; set; }
    }

    public class LogMap
    {
        private DBItem row;
        private List<DBLogItem> logs = new List<DBLogItem>();
        private readonly SelectableList<LogEntry> changes = new SelectableList<LogEntry>();
        private string user;
        private DBTable table;
        private string text;

        public LogMap()
        {
            changes.Indexes.Add(new ActionInvoker<LogEntry, DBColumn>(nameof(LogEntry.Column),
                        (item) => item.Column,
                        (item, value) => item.Column = value));
        }

        public LogMap(DBItem row) : this()
        {
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
            QQuery query = new QQuery(string.Empty, (DBTable)Row.Table.LogTable);
            query.BuildParam(Row.Table.LogTable.BaseKey, CompareType.Equal, row.PrimaryId);
            query.BuildParam(Row.Table.LogTable.StatusKey, CompareType.Equal, (int)DBStatus.New);
            Logs.AddRange(Row.Table.LogTable.LoadItems(query, DBLoadParam.Load | DBLoadParam.Synchronize).Cast<DBLogItem>());

            RefreshChanges();
        }

        public List<DBLogItem> Logs
        {
            get { return logs; }
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
            logs.Sort(new DBComparer<DBLogItem, int?>(Row.Table.LogTable.PrimaryKey));

            changes.Clear();
            user = string.Empty;
            DBLogItem prev = null;
            foreach (var log in Logs)
            {
                if (log.Status == DBStatus.New)
                {
                    string name = log.UserReg?.DBUser?.Name;
                    if (user.IndexOf(name, StringComparison.Ordinal) < 0)
                        user += name + "; ";
                    foreach (var logColumn in log.LogTable.GetLogColumns())
                    {
                        LogEntry map = changes.SelectOne(nameof(LogEntry.Column), CompareType.Equal, logColumn.BaseColumn);
                        if (map == null)
                        {
                            map = new LogEntry
                            {
                                User = name,
                                Column = logColumn.BaseColumn,
                                Old = prev?.GetValue(logColumn)
                            };
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

        public SelectableList<LogEntry> Changes
        {
            get { return changes; }
        }

        //public bool Check()
        //{
        //    bool flag = true;
        //    foreach (var log in Logs)
        //    {
        //        if (log.Status == DBStatus.New && (((IUserLog)log.UserLog)?.User?.IsCurrent ?? false))// && !log.User.Super
        //        {
        //            flag = false;
        //            break;
        //        }
        //    }
        //    return flag;
        //}

        public async Task Accept(IUserIdentity user)
        {
            await DBLogItem.Accept(row, logs, user);
            RefreshLogs();
        }

        public async Task Reject(IUserIdentity user)
        {
            await DBLogItem.Reject(logs, user);
            RefreshLogs();
        }
    }


}
