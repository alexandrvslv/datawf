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
                    string name = log.GetUser()?.Name;
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
