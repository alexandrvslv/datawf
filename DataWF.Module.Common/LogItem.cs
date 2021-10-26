using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;

[assembly: Invoker(typeof(LogItem), nameof(LogItem.UserReg), typeof(LogItem.UserRegInvoker<>))]
[assembly: Invoker(typeof(LogItem), nameof(LogItem.LogUserId), typeof(LogItem.LogUserIdInvoker<>))]
[assembly: Invoker(typeof(LogItem), nameof(LogItem.LogUser), typeof(LogItem.LogUserInvoker<>))]
namespace DataWF.Module.Common
{
    public abstract class LogItem : DBLogItem
    {
        private UserReg userReg;

        [Reference(nameof(UserRegId))]
        public UserReg UserReg
        {
            get => userReg ?? (GetReference<UserReg>(LogTable.UserLogKey, ref userReg));
            set => SetReference(userReg = value, LogTable.UserLogKey);
        }

        [Column("loguser_id", ColumnType = DBColumnTypes.Code)]
        public int? LogUserId
        {
            get => UserReg?.UserId;
            set { }
        }

        [Reference(nameof(LogUserId))]
        public User LogUser
        {
            get => UserReg?.User;
            set { }
        }


        public class UserRegInvoker<T> : Invoker<T, UserReg> where T : LogItem
        {
            public override string Name => nameof(LogItem.UserReg);

            public override bool CanWrite => true;

            public override UserReg GetValue(T target) => target.UserReg;

            public override void SetValue(T target, UserReg value) => target.UserReg = value;
        }

        public class LogUserIdInvoker<T> : Invoker<T, int?> where T : LogItem
        {
            public override string Name => nameof(LogItem.LogUserId);

            public override bool CanWrite => false;

            public override int? GetValue(T target) => target.LogUserId;

            public override void SetValue(T target, int? value) { }
        }

        public class LogUserInvoker<T> : Invoker<T, User> where T : LogItem
        {
            public override string Name => nameof(LogItem.LogUser);

            public override bool CanWrite => false;

            public override User GetValue(T target) => target.LogUser;

            public override void SetValue(T target, User value) { }
        }
    }
}
