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


        [Invoker(typeof(LogItem), nameof(LogItem.UserReg))]
        public class UserRegInvoker<T> : Invoker<T, UserReg> where T : LogItem
        {
            public override string Name => nameof(LogItem.UserReg);

            public override bool CanWrite => true;

            public override UserReg GetValue(T target) => target.UserReg;

            public override void SetValue(T target, UserReg value) => target.UserReg = value;
        }

        [Invoker(typeof(LogItem), nameof(LogItem.LogUserId))]
        public class LogUserIdInvoker<T> : Invoker<T, int?> where T : LogItem
        {
            public override string Name => nameof(LogItem.LogUserId);

            public override bool CanWrite => false;

            public override int? GetValue(T target) => target.LogUserId;

            public override void SetValue(T target, int? value) { }
        }

        [Invoker(typeof(LogItem), nameof(LogItem.LogUser))]
        public class LogUserInvoker<T> : Invoker<T, User> where T : LogItem
        {
            public override string Name => nameof(LogItem.LogUser);

            public override bool CanWrite => false;

            public override User GetValue(T target) => target.LogUser;

            public override void SetValue(T target, User value) { }
        }
    }
}
