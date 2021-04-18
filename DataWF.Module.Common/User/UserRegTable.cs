using DataWF.Data;
using System;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{
    public partial class UserRegTable
    {
        public static UserRegStrategy LogStrategy = UserRegStrategy.BySession;

        public event EventHandler<DBItemEventArgs> RowLoging;
        public event EventHandler<DBItemEventArgs> RowLoged;

        public async ValueTask OnDBItemLoging(DBItemEventArgs arg)
        {
            if (arg.Item.Table == this
                || arg.Item.Table is IDBLogTable
                || arg.Item.Schema != Schema)
                return;
            var user = arg.User as User;
            RowLoging?.Invoke(null, arg);
            if (user != null && user.LogStart == null)
            {
                await ((UserTable)Schema.GetTable<User>()).RegisterSession(user);
            }
            var userLog = user?.LogStart;

            if (LogStrategy == UserRegStrategy.ByTransaction)
            {
                if (arg.Transaction != null)
                {
                    if (arg.Transaction.UserLog == null)
                    {
                        arg.Transaction.UserLog = new UserReg(this) { User = user, Parent = userLog, RegType = UserRegType.Transaction };
                        await arg.Transaction.UserLog.Save(arg.Transaction);
                    }
                    userLog = (UserReg)arg.Transaction.UserLog;
                }
            }
            else if (LogStrategy == UserRegStrategy.ByItem)
            {
                userLog = new UserReg(this) { User = user, Parent = userLog, RegType = UserRegType.Transaction };
                await userLog.Save(arg.Transaction);
            }
            if (arg.LogItem is LogItem logItem)
            {
                logItem.UserReg = userLog;
            }
            RowLoged?.Invoke(null, arg);
        }

        public async Task<UserReg> LogUser(User user, UserRegType type, string info)
        {
            var newLog = new UserReg(this)
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
