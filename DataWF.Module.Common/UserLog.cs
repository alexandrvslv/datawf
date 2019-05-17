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
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Threading.Tasks;

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

    [DataContract, Table("duser_log", "User", BlockSize = 500, IsLoging = false)]
    public class UserLog : DBGroupItem, IUserLog
    {
        private static DBColumn userKey = DBColumn.EmptyKey;
        private static DBColumn logTypeKey = DBColumn.EmptyKey;
        private static DBColumn redoKey = DBColumn.EmptyKey;
        private static DBColumn textDataKey = DBColumn.EmptyKey;
        private static DBTable<UserLog> dbTable;
        public static UserLogStrategy LogStrategy = UserLogStrategy.BySession;
        private User user;
        private UserLog redo;

        public static DBColumn UserKey => DBTable.ParseProperty(nameof(UserId), ref userKey);
        public static DBColumn LogTypeKey => DBTable.ParseProperty(nameof(LogType), ref logTypeKey);
        public static DBColumn RedoKey => DBTable.ParseProperty(nameof(RedoId), ref redoKey);
        public static DBColumn TextDataKey => DBTable.ParseProperty(nameof(TextData), ref textDataKey);
        public static DBTable<UserLog> DBTable => dbTable ?? (dbTable = GetTable<UserLog>());
        public static event EventHandler<DBItemEventArgs> RowLoging;
        public static event EventHandler<DBItemEventArgs> RowLoged;

        public static async void OnDBItemLoging(DBItemEventArgs arg)
        {
            if (arg.Item.Table == UserLog.DBTable || arg.Item.Table is DBLogTable)
                return;
            var user = arg.User as User;
            RowLoging?.Invoke(null, arg);
            if (user != null && user.LogStart == null)
            {
                await User.StartSession(user);
            }
            var userLog = user?.LogStart;

            if (LogStrategy == UserLogStrategy.ByTransaction)
            {
                if (arg.Transaction != null)
                {
                    if (arg.Transaction.UserLog == null)
                    {
                        arg.Transaction.UserLog = new UserLog { User = user, Parent = userLog, LogType = UserLogType.Transaction };
                        await arg.Transaction.UserLog.Save(arg.Transaction);
                    }
                    userLog = (UserLog)arg.Transaction.UserLog;
                }
            }
            else if (LogStrategy == UserLogStrategy.ByItem)
            {
                userLog = new UserLog { User = user, Parent = userLog, LogType = UserLogType.Transaction };
                await userLog.Save(arg.Transaction);
            }
            if (arg.LogItem != null)
            {
                arg.LogItem.UserLog = userLog;
            }
            RowLoged?.Invoke(null, arg);
        }

        public UserLog()
        { }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("user_id", Keys = DBColumnKeys.View)]
        public int? UserId
        {
            get { return GetValue<int?>(UserKey); }
            set { SetValue(value, UserKey); }
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get { return GetReference(UserKey, ref user); }
            set { SetReference(user = value, UserKey); }
        }

        [DataMember, Column("type_id", Keys = DBColumnKeys.ElementType | DBColumnKeys.View)]
        public UserLogType? LogType
        {
            get { return GetValue<UserLogType?>(LogTypeKey); }
            set { SetValue(value, LogTypeKey); }
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
            get { return GetValue<long?>(RedoKey); }
            set { SetValue(value, RedoKey); }
        }

        [Reference(nameof(RedoId))]
        public UserLog Redo
        {
            get { return GetReference(RedoKey, ref redo); }
            set { SetReference(redo = value, RedoKey); }
        }

        [DataMember, Column("text_data")]
        public string TextData
        {
            get { return GetValue<string>(TextDataKey); }
            set { SetValue(value, TextDataKey); }
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

        public static async Task LogUser(User user, UserLogType type, string info)
        {
            var newLog = new UserLog()
            {
                User = user,
                LogType = type,
                Parent = user.LogStart
            };

            if (type == UserLogType.Authorization)
            {
                user.LogStart = newLog;
                //var prop = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();

                //string address = string.Empty;
                //foreach (var ip in System.Net.Dns.GetHostAddresses(prop.HostName))
                //    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                //        address += ip + "; ";
                //text = string.Format("{0} on {1}-{2}({3})", info, prop.DomainName, prop.HostName, address);
            }

            newLog.TextData = info;
            await newLog.Save(user);
        }

        DBItem IUserLog.User
        {
            get { return User; }
        }
    }
}
