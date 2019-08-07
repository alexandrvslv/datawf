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
using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{
    public enum UserRegType
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

    public enum UserRegStrategy
    {
        ByItem,
        ByTransaction,
        BySession
    }

    [DataContract, Table("duser_log", "User", BlockSize = 500, IsLoging = false)]
    public class UserReg : DBUserReg
    {
        private static DBColumn userKey = DBColumn.EmptyKey;
        private static DBColumn regTypeKey = DBColumn.EmptyKey;
        private static DBColumn redoKey = DBColumn.EmptyKey;
        private static DBColumn textDataKey = DBColumn.EmptyKey;
        private static DBTable<UserReg> dbTable;
        public static UserRegStrategy LogStrategy = UserRegStrategy.BySession;
        private User user;
        private UserReg redo;

        public static DBColumn UserKey => DBTable.ParseProperty(nameof(UserId), ref userKey);
        public static DBColumn RegTypeKey => DBTable.ParseProperty(nameof(RegType), ref regTypeKey);
        public static DBColumn RedoKey => DBTable.ParseProperty(nameof(RedoId), ref redoKey);
        public static DBColumn TextDataKey => DBTable.ParseProperty(nameof(TextData), ref textDataKey);
        public static DBTable<UserReg> DBTable => dbTable ?? (dbTable = GetTable<UserReg>());
        public static event EventHandler<DBItemEventArgs> RowLoging;
        public static event EventHandler<DBItemEventArgs> RowLoged;

        public static async void OnDBItemLoging(DBItemEventArgs arg)
        {
            if (arg.Item.Table == UserReg.DBTable || arg.Item.Table is IDBLogTable)
                return;
            var user = arg.User as User;
            RowLoging?.Invoke(null, arg);
            if (user != null && user.LogStart == null)
            {
                await Common.User.StartSession(user);
            }
            var userLog = user?.LogStart;

            if (LogStrategy == UserRegStrategy.ByTransaction)
            {
                if (arg.Transaction != null)
                {
                    if (arg.Transaction.UserLog == null)
                    {
                        arg.Transaction.UserLog = new UserReg { User = user, Parent = userLog, RegType = UserRegType.Transaction };
                        await arg.Transaction.UserLog.Save(arg.Transaction);
                    }
                    userLog = (UserReg)arg.Transaction.UserLog;
                }
            }
            else if (LogStrategy == UserRegStrategy.ByItem)
            {
                userLog = new UserReg { User = user, Parent = userLog, RegType = UserRegType.Transaction };
                await userLog.Save(arg.Transaction);
            }
            if (arg.LogItem != null)
            {
                arg.LogItem.UserReg = userLog;
            }
            RowLoged?.Invoke(null, arg);
        }

        public UserReg()
        { }
        
        public override long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }
        
        public override int? UserId
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

        public override DBUser DBUser { get => User; set => User = (User)value; }


        [DataMember, Column("type_id", Keys = DBColumnKeys.ElementType | DBColumnKeys.View)]
        public UserRegType? RegType
        {
            get { return GetValue<UserRegType?>(RegTypeKey); }
            set { SetValue(value, RegTypeKey); }
        }

        [Browsable(false)]
        [DataMember, Column("parent_id", Keys = DBColumnKeys.Group)]
        public long? ParentId
        {
            get { return GetGroupValue<long?>(); }
            set { SetGroupValue(value); }
        }

        [Reference(nameof(ParentId))]
        public UserReg Parent
        {
            get { return GetGroupReference<UserReg>(); }
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
        public UserReg Redo
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

        public List<UserRegItem> Items { get; set; }

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

        public static async Task LogUser(User user, UserRegType type, string info)
        {
            var newLog = new UserReg()
            {
                User = user,
                RegType = type,
                Parent = user.LogStart
            };

            if (type == UserRegType.Authorization)
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


    }
}
