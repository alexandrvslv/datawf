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

    [Table("duser_log", "User", BlockSize = 500, Keys = DBTableKeys.NoLogs)]
    public class UserReg : DBUserReg
    {
        public static UserRegStrategy LogStrategy = UserRegStrategy.BySession;

        public static readonly DBTable<UserReg> DBTable = GetTable<UserReg>();
        public static readonly DBColumn UserKey = DBTable.ParseProperty(nameof(UserId));
        public static readonly DBColumn RegTypeKey = DBTable.ParseProperty(nameof(RegType));
        public static readonly DBColumn RedoKey = DBTable.ParseProperty(nameof(RedoId));
        public static readonly DBColumn TextDataKey = DBTable.ParseProperty(nameof(TextData));
        public static event EventHandler<DBItemEventArgs> RowLoging;
        public static event EventHandler<DBItemEventArgs> RowLoged;

        private User user;
        private UserReg redo;

        public static async ValueTask OnDBItemLoging(DBItemEventArgs arg)
        {
            if (arg.Item.Table == UserReg.DBTable || arg.Item.Table is IDBLogTable)
                return;
            var user = arg.User as User;
            RowLoging?.Invoke(null, arg);
            if (user != null && user.LogStart == null)
            {
                await Common.User.RegisterSession(user);
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
            if (arg.LogItem is LogItem logItem)
            {
                logItem.UserReg = userLog;
            }
            RowLoged?.Invoke(null, arg);
        }

        public UserReg()
        { }

        public override long Id
        {
            get => GetValue<long>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        public override int? UserId
        {
            get => GetValue<int?>(UserKey);
            set => SetValue(value, UserKey);
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get => GetReference(UserKey, ref user);
            set => SetReference(user = value, UserKey);
        }

        public override DBUser DBUser
        {
            get => User; set => User = (User)value;
        }

        [Column("type_id", Keys = DBColumnKeys.ElementType | DBColumnKeys.View)]
        public UserRegType? RegType
        {
            get => GetValue<UserRegType?>(RegTypeKey);
            set => SetValue(value, RegTypeKey);
        }

        [Browsable(false)]
        [Column("parent_id", Keys = DBColumnKeys.Group)]
        public long? ParentId
        {
            get => GetGroupValue<long?>();
            set => SetGroupValue(value);
        }

        [Reference(nameof(ParentId))]
        public UserReg Parent
        {
            get => GetGroupReference<UserReg>();
            set => SetGroupReference(value);
        }

        [Browsable(false)]
        [Column("redo_id")]
        public long? RedoId
        {
            get => GetValue<long?>(RedoKey);
            set => SetValue(value, RedoKey);
        }

        [Reference(nameof(RedoId))]
        public UserReg Redo
        {
            get => GetReference(RedoKey, ref redo);
            set => SetReference(redo = value, RedoKey);
        }

        [Column("text_data")]
        public string TextData
        {
            get => GetValue<string>(TextDataKey);
            set => SetValue(value, TextDataKey);
        }

        public List<UserRegItem> Items { get; set; }

        protected override void RaisePropertyChanged(string property)
        {
            base.RaisePropertyChanged(property);
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

        public static async Task<UserReg> LogUser(User user, UserRegType type, string info)
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
            return newLog;
        }


    }
}
