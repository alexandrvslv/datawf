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
using System.Text;
using DataWF.Data;
using DataWF.Common;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Linq;
using System.Runtime.Serialization;
using System.Collections;

namespace DataWF.Module.Common
{
    public enum UserLogType
    {
        None,
        Password,
        Authorization,
        Start,
        Stop,
        Execute,
        Transaction,
        Reject
    }

    public enum UserLogStrategy
    {
        ByItem,
        ByTransaction,
        BySession
    }

    public class UserLogItem
    {
        private DBLogItem cacheLogItem;
        private DBTable cacheTargetTable;
        private DBItem cacheTarget;

        [Browsable(false)]
        public string TableName { get; set; }

        [XmlIgnore]
        public DBTable Table
        {
            get { return cacheTargetTable ?? (cacheTargetTable = DBService.ParseTable(TableName)); }
            set
            {
                cacheTargetTable = value;
                TableName = value?.FullName;
            }
        }

        [Browsable(false)]
        public string ItemId { get; set; }

        [XmlIgnore]
        public DBItem Item
        {
            get { return cacheTarget ?? (cacheTarget = (Table?.LoadItemById(ItemId) ?? DBItem.EmptyItem)); }
            set
            {
                cacheTarget = value;
                ItemId = value?.PrimaryId.ToString();
                Table = value?.Table;
            }
        }

        public DBLogTable LogTable
        {
            get { return Table?.LogTable; }
        }

        [Browsable(false)]
        public int? LogId { get; set; }

        [XmlIgnore]
        public DBLogItem LogItem
        {
            get { return cacheLogItem ?? (cacheLogItem = LogTable?.LoadById(LogId)); }
            set
            {
                cacheLogItem = value;
                if (value != null)
                {
                    LogId = value.LogId;
                    Item = value.BaseItem;
                }
            }
        }

        public void RefereshText()
        {
            string _textCache = "";
            if (_textCache?.Length == 0 && LogItem != null)
            {
                var logPrevius = LogItem.GetPrevius();
                foreach (var logColumn in LogTable.GetLogColumns())
                {
                    var column = logColumn.BaseColumn;
                    if ((column.Keys & DBColumnKeys.Stamp) == DBColumnKeys.Stamp)
                        continue;
                    var oldValue = logPrevius?.GetValue(logColumn);
                    var newValue = LogItem.GetValue(logColumn);
                    if (DBService.Equal(oldValue, newValue))
                        continue;
                    var oldFormat = column.Access.View ? column.FormatValue(oldValue) : "*****";
                    var newFormat = column.Access.View ? column.FormatValue(newValue) : "*****";
                    if (oldValue == null && newValue != null)
                        _textCache += string.Format("{0}: {1}\n", column, newFormat);
                    else if (oldValue != null && newValue == null)
                        _textCache += string.Format("{0}: {1}\n", column, oldFormat);
                    else if (oldValue != null && newValue != null)
                    {
                        if ((column.Keys & DBColumnKeys.Access) == DBColumnKeys.Access)
                        {
                            string rez = string.Empty;
                            AccessValue oldAcces = new AccessValue((byte[])oldValue);
                            AccessValue newAcces = new AccessValue((byte[])newValue);
                            foreach (var oAccess in oldAcces.Items)
                            {
                                int index = newAcces.GetIndex(oAccess.Group);
                                if (index < 0)
                                    rez += string.Format("Remove {0}; ", oAccess);
                                else if (!newAcces.Items[index].Equals(oAccess))
                                {
                                    var nAceess = newAcces.Items[index];
                                    rez += string.Format("Change {0}({1}{2}{3}{4}{5}{6}); ", oAccess.Group,
                                        oAccess.View != nAceess.View ? (nAceess.View ? "+" : "-") + "View " : "",
                                        oAccess.Edit != nAceess.Edit ? (nAceess.Edit ? "+" : "-") + "Edit " : "",
                                        oAccess.Create != nAceess.Create ? (nAceess.Create ? "+" : "-") + "Create " : "",
                                        oAccess.Delete != nAceess.Delete ? (nAceess.Delete ? "+" : "-") + "Delete " : "",
                                        oAccess.Admin != nAceess.Admin ? (nAceess.Admin ? "+" : "-") + "Admin " : "",
                                        oAccess.Accept != nAceess.Accept ? (nAceess.Accept ? "+" : "-") + "Accept" : "");
                                }
                            }
                            foreach (var nAccess in newAcces.Items)
                            {
                                int index = oldAcces.GetIndex(nAccess.Group);
                                if (index < 0)
                                    rez += string.Format("New {0}; ", nAccess);
                            }
                            _textCache += string.Format("{0}: {1}", column, rez);
                        }
                        else if (column.DataType == typeof(string))
                        {
                            var diffs = DiffResult.DiffLines(oldFormat, newFormat);
                            var buf = new StringBuilder();
                            foreach (var diff in diffs)
                            {
                                string val = diff.Result.Trim();
                                if (val.Length > 0)
                                    buf.AppendLine(string.Format("{0} {1} at {2};", diff.Type, val, diff.Index, diff.Index));
                            }
                            _textCache += string.Format("{0}: {1}\r\n", column, buf);
                        }
                        else
                            _textCache += string.Format("{0}: Old:{1} New:{2}\r\n", column, oldFormat, newFormat);
                    }
                }
            }
        }
    }

    [DataContract, Table("duser_log", "User", BlockSize = 500, IsLoging = false)]
    public class UserLog : DBGroupItem, IUserLog
    {
        public static UserLogStrategy LogStrategy = UserLogStrategy.BySession;

        [ThreadStatic]
        public static UserLog CurrentLog;

        public static DBTable<UserLog> DBTable
        {
            get { return GetTable<UserLog>(); }
        }

        public static event EventHandler<DBItemEventArgs> RowLoging;

        public static event EventHandler<DBItemEventArgs> RowLoged;

        public static void OnDBItemLoging(DBItemEventArgs arg)
        {
            if (arg.Item.Table == UserLog.DBTable || arg.Item.Table is DBLogTable)
                return;
            RowLoging?.Invoke(null, arg);
            var userLog = CurrentLog ?? User.CurrentUser?.LogStart;

            if (LogStrategy == UserLogStrategy.ByTransaction)
            {
                var transaction = DBTransaction.Current;
                if (transaction.UserLog == null)
                {
                    transaction.UserLog = new UserLog { User = User.CurrentUser, Parent = userLog, LogType = UserLogType.Transaction };
                    transaction.UserLog.Save();
                }
                userLog = (UserLog)transaction.UserLog;

            }
            else if (LogStrategy == UserLogStrategy.ByItem)
            {
                userLog = new UserLog { User = User.CurrentUser, Parent = userLog, LogType = UserLogType.Transaction };
                userLog.Save();
            }
            if (arg.LogItem != null)
            {
                arg.LogItem.UserLog = userLog;
            }
            RowLoged?.Invoke(null, arg);
        }

        public UserLog()
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("user_id", Keys = DBColumnKeys.View)]
        public int? UserId
        {
            get { return GetProperty<int?>(nameof(UserId)); }
            set { SetProperty(value, nameof(UserId)); }
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get { return GetPropertyReference<User>(); }
            set { SetPropertyReference(value); }
        }

        [DataMember, Column("type_id", Keys = DBColumnKeys.ElementType | DBColumnKeys.View)]
        public UserLogType? LogType
        {
            get { return GetProperty<UserLogType?>(nameof(LogType)); }
            set { SetProperty(value, nameof(LogType)); }
        }

        [Browsable(false)]
        [DataMember, Column("parent_id", Keys = DBColumnKeys.Group)]
        public long? ParentId
        {
            get { return GetGroupValue<long?>(); }
            set { SetGroupValue(value); }
        }

        [Reference(nameof(ParentId))]
        public UserLog Parent
        {
            get { return GetGroupReference<UserLog>(); }
            set { SetGroupReference(value); }
        }

        [Browsable(false)]
        [DataMember, Column("redo_id")]
        public long? RedoId
        {
            get { return GetProperty<long?>(nameof(RedoId)); }
            set { SetProperty(value, nameof(RedoId)); }
        }

        [Reference(nameof(RedoId))]
        public UserLog Redo
        {
            get { return GetPropertyReference<UserLog>(); }
            set { SetPropertyReference(value); }
        }

        [DataMember, Column("text_data")]
        public string TextData
        {
            get { return GetProperty<string>(nameof(TextData)); }
            set { SetProperty(value, nameof(TextData)); }
        }

        public List<UserLogItem> Items { get; set; }

        public override void OnPropertyChanged(string property, DBColumn column = null, object value = null)
        {
            base.OnPropertyChanged(property, column, value);
        }



        //public SelectableList<LogChange> GetLogMapList()
        //{
        //    DBTable table = TargetTable;
        //    SelectableList<LogChange> listmap = new SelectableList<LogChange>();
        //    if (LogItem != null)
        //    {
        //        foreach (var column in TargetTable.Columns)
        //        {
        //            var logColumn = LogTable.GetLogColumn(column);
        //            if (logColumn != null)
        //            {
        //                LogChange field = new LogChange();
        //                field.Column = column;
        //                field.Old = OldLogItem?.GetValue(logColumn);
        //                field.New = LogItem?.GetValue(logColumn);
        //                listmap.Add(field);
        //            }
        //        }
        //    }
        //    return listmap;
        //}

        public static void LogUser(User user, UserLogType type, string info)
        {
            var newLog = new UserLog()
            {
                User = user,
                LogType = type,
                Parent = user.LogStart
            };

            string text = info;
            if (type == UserLogType.Authorization)
            {
                user.LogStart = newLog;
                var prop = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();

                string address = string.Empty;
                foreach (var ip in System.Net.Dns.GetHostAddresses(prop.HostName))
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        address += ip + "; ";
                text = string.Format("{0} on {1}-{2}({3})", info, prop.DomainName, prop.HostName, address);
            }

            newLog.TextData = text;
            newLog.Save();
        }

        DBItem IUserLog.User
        {
            get { return User; }
        }
    }
}
