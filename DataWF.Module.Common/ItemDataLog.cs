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
        private List<DataLog> logs;
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
            QQuery query = new QQuery(string.Empty, DataLog.DBTable);
            query.BuildPropertyParam(nameof(DataLog.TargetTable), CompareType.Equal, row.Table.FullName);
            query.BuildPropertyParam(nameof(DataLog.TargetId), CompareType.Equal, row.PrimaryId);
            query.BuildPropertyParam(nameof(DataLog.DBState), CompareType.Equal, DBStatus.New);
            Logs.AddRange(DataLog.DBTable.Load(query, DBLoadParam.Load | DBLoadParam.Synchronize));
        }

        public List<DataLog> Logs
        {
            get
            {
                if (logs == null)
                    logs = new List<DataLog>();//DataLog.DBTable);
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
            logs.Sort(new DBComparer(DataLog.DBTable.DateKey));

            changes.Clear();
            user = string.Empty;
            foreach (DataLog log in Logs)
            {
                if (log.Status == DBStatus.New)
                {
                    string name = log.User == null ? "null" : log.User.Name;
                    if (user.IndexOf(name, StringComparison.Ordinal) < 0)
                        user += name + "; ";
                    Dictionary<string, object> vals = log.NewMap == null ? log.OldMap : log.NewMap;
                    if (vals != null)
                    {
                        foreach (KeyValuePair<string, object> item in vals)
                        {
                            DBColumn Column = table.ParseColumn(item.Key);
                            LogChange map = changes.Find("Column", CompareType.Equal, Column);
                            if (map == null)
                            {
                                map = new LogChange();
                                map.Column = Column;
                                map.Old = log.OldMap != null && log.OldMap.ContainsKey(item.Key) ? log.OldMap[item.Key] : null;
                                changes.Add(map);
                            }
                            if (!map.User.Contains(name))
                                map.User += name + "; ";
                            map.New = log.NewMap != null && log.NewMap.ContainsKey(item.Key) ? log.NewMap[item.Key] : null;
                        }
                    }
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
            foreach (DataLog log in Logs)
                if (log.Status == DBStatus.New && log.User.IsCurrent)// && !log.User.Super
                {
                    flag = false;
                    break;
                }

            return flag;
        }

        public void Accept()
        {
            DataLog.Accept(row, logs);
            RefreshLogs();
        }

        public void Reject()
        {
            DataLog.Reject(logs);
            RefreshLogs();
        }


        public List<DataLog> GetChilds()
        {
            var query = new QQuery("", DataLog.DBTable);
            query.BuildPropertyParam(nameof(DataLog.ParentId), CompareType.In, logs);
            return DataLog.DBTable.Load(query);
        }
    }
}
