using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;

namespace DataWF.Module.Common
{
    [InvokerGenerator(Instance = false)]
    public abstract partial class LogItem : DBLogItem
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
    }
}
