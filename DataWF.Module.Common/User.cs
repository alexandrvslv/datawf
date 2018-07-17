/*
 User.cs
 
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
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Linq;
using Novell.Directory.Ldap;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security;
using MailKit.Net.Smtp;
using System.Net;
using System.Security.Principal;

namespace DataWF.Module.Common
{

    [DataContract, Table("ruser", "User", BlockSize = 100)]
    public class User : DBItem, IComparable, IDisposable, IIdentity
    {
        [ThreadStatic]
        private static User threadCurrentUser;
        private static User currentUser;

        public static Action CurrentUserChanged;


        public static User CurrentUser
        {
            get { return threadCurrentUser ?? currentUser; }
        }

        public static void SetCurrentUser(User value, bool threaded = false)
        {
            if (CurrentUser == value)
                return;
            if (threaded)
                threadCurrentUser = value;
            else
                currentUser = value;
            if (value != null)
            {
                UserLog.LogUser(value, UserLogType.Authorization, "GetUser");
                CurrentUserChanged?.Invoke();
            }
        }

        public static User SetCurrentByEnvironment()
        {
            var user = DBTable.LoadByCode(Environment.UserName);
            SetCurrentUser(user);
            return user;
        }

        public static User SetCurrentByCredential(string login, string password, bool threaded = false)
        {
            var user = GetUser(login, GetSha(password));
            SetCurrentUser(user ?? throw new KeyNotFoundException("User not found!"), threaded);
            return user;
        }

        public static User SeCurrentByEmail(string email, SecureString password, bool threaded = false)
        {
            return SetCurrentByEmail(new NetworkCredential(email, password), threaded);
        }

        public static void SetCurrentByEmail(string email, bool threaded = false)
        {
            var user = DBTable.SelectOne(DBTable.ParseProperty(nameof(EMail)), email);
            if (user == null)
                throw new KeyNotFoundException("User not found!");
            SetCurrentUser(user, threaded);
        }

        public static User SetCurrentByEmail(NetworkCredential credentials, bool threaded = false)
        {
            var user = DBTable.SelectOne(DBTable.ParseProperty(nameof(EMail)), credentials.UserName);
            if (user == null)
                throw new KeyNotFoundException("User not found!");
            var config = SmtpSetting.Load();
            using (var smtpClient = new SmtpClient())
            {
                smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                smtpClient.Connect(config.Host, config.Port, config.SSL);
                smtpClient.Authenticate(credentials);
                SetCurrentUser(user, threaded);
            }
            return user;
        }

        public static DBTable<User> DBTable
        {
            get { return GetTable<User>(); }
        }

        private static string GetString(byte[] data)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                builder.Append(data[i].ToString("x2"));
            }
            return builder.ToString();
        }

        private static string GetSha(string input)
        {
            if (input == null)
                return null;

            return GetString(SHA1.Create().ComputeHash(Encoding.Default.GetBytes(input)));
        }

        private static string GetMd5(string input)
        {
            if (input == null)
                return null;

            return GetString(MD5.Create().ComputeHash(Encoding.Default.GetBytes(input)));
        }

        private static UserPasswordSpec PasswordSpec = UserPasswordSpec.Lenght6 | UserPasswordSpec.CharSpecial | UserPasswordSpec.CharNumbers;

        public static string ValidateText(User User, string password, bool checkOld)
        {
            string message = string.Empty;
            if (password == null)
                return message;
            if (PasswordSpec.HasFlag(UserPasswordSpec.Lenght6) && password.Length < 6)
                message += Locale.Get("Login", " Must be more than 6 characters long.");
            if (PasswordSpec.HasFlag(UserPasswordSpec.Lenght8) && password.Length < 8)
                message += Locale.Get("Login", " Must be more than 8 characters long.");
            if (PasswordSpec.HasFlag(UserPasswordSpec.Lenght10) && password.Length < 10)
                message += Locale.Get("Login", " Must be more than 10 characters long.");
            if (PasswordSpec.HasFlag(UserPasswordSpec.CharNumbers) && !Regex.IsMatch(password, @"(?=.*\d)^.*", RegexOptions.CultureInvariant))
                message += Locale.Get("Login", " Should contain a number.");
            if (PasswordSpec.HasFlag(UserPasswordSpec.CharUppercase) && !Regex.IsMatch(password, @"(?=.*[A-Z])^.*", RegexOptions.CultureInvariant))
                message += Locale.Get("Login", " Should contain a uppercase.");
            if (PasswordSpec.HasFlag(UserPasswordSpec.CharLowercase) && !Regex.IsMatch(password, @"(?=.*[a-z])^.*", RegexOptions.CultureInvariant))
                message += Locale.Get("Login", " Should contain a lowercase letters.");
            if (PasswordSpec.HasFlag(UserPasswordSpec.CharSpecial) && !Regex.IsMatch(password, @"(?=.*[^A-Za-z0-9])^.*", RegexOptions.CultureInvariant))
                message += Locale.Get("Login", " Should contain a special character.");
            if (PasswordSpec.HasFlag(UserPasswordSpec.CharRepet) && Regex.IsMatch(password, @"(.)\1{2,}", RegexOptions.CultureInvariant))
                message += Locale.Get("Login", " Remove repeted characters.");
            if (PasswordSpec.HasFlag(UserPasswordSpec.Login) && User.Login != null && password.IndexOf(User.Login, StringComparison.OrdinalIgnoreCase) >= 0)
                message += Locale.Get("Login", " Should not contain your Login.");

            var proc = User.Table.Schema?.Procedures["simple"];
            if (proc != null)
            {
                string[] split = proc.Source.Split('\r', '\n');
                foreach (string s in split)
                {
                    if (s.Length > 0 && password.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        message += Locale.Get("Login", " Should not contain simple words.");
                        break;
                    }
                }
            }

            if (checkOld)
            {
                string encoded = GetSha(password);
                var list = GetOld(User);
                foreach (var item in list)
                    if (item.TextData == encoded)
                    {
                        message += Locale.Get("Login", " Password was same before.");
                        break;
                    }
            }

            return message;
        }

        public static IEnumerable<UserLog> GetOld(User User)
        {
            var filter = new QQuery(string.Empty, UserLog.DBTable);
            filter.BuildPropertyParam(nameof(UserLog.UserId), CompareType.Equal, User.PrimaryId);
            filter.BuildPropertyParam(nameof(UserLog.LogType), CompareType.Equal, UserLogType.Password);
            filter.Orders.Add(new QOrder { Column = UserLog.DBTable.ParseProperty(nameof(UserLog.Id)), Direction = ListSortDirection.Descending });
            return UserLog.DBTable.Load(filter, DBLoadParam.Load | DBLoadParam.Synchronize);
        }

        public static User GetUser(string login, string passoword)
        {
            var query = new QQuery(string.Empty, User.DBTable);
            query.BuildPropertyParam(nameof(Login), CompareType.Equal, login);
            query.BuildPropertyParam(nameof(Password), CompareType.Equal, passoword);
            var user = User.DBTable.Select(query).FirstOrDefault();
            if (user != null)
                UserLog.LogUser(user, UserLogType.Authorization, "GetUser");
            return user;
        }

        protected bool online = false;

        public User()
        {
            Build(DBTable);
        }

        public UserLog LogStart;

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("login", 256, Keys = DBColumnKeys.Code | DBColumnKeys.Indexing), Index("ruser_login", true)]
        public string Login
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { this[Table.CodeKey] = value; }
        }

        [DataMember, Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string Name
        {
            get { return GetName(); }
            set { SetName(value); }
        }

        [DataMember, Column("department_id"), Browsable(false)]
        public int? DepartmentId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get { return GetPropertyReference<Department>(); }
            set { SetPropertyReference(value); }
        }

        [DataMember, Column("position_id"), Browsable(false)]
        public int? PositionId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get { return GetPropertyReference<Position>(); }
            set
            {
                SetPropertyReference(value);
                Department = value?.Department;
            }
        }

        [ReadOnly(true)]
        [DataMember, Column("super", Default = "False", Keys = DBColumnKeys.Notnull)]
        public bool? Super
        {
            get { return GetProperty<bool?>(); }
            set { SetProperty(value); }
        }

        [Browsable(false)]
        public bool Online
        {
            get { return online; }
            set
            {
                if (online == value)
                    return;
                online = value;
                OnPropertyChanged(nameof(Status), null);
                //OnPropertyChanged("Online");
            }
        }

        [DataMember, Column("email", 1024), Index("ruser_email", true)]
        public string EMail
        {
            get { return GetProperty<string>(nameof(EMail)); }
            set { SetProperty(value, nameof(EMail)); }
        }

        public bool IsBlock
        {
            get { return Status != DBStatus.Actual; }
            set { Status = value ? DBStatus.Actual : DBStatus.Error; }
        }

        [DataMember, Column("password", 256, Keys = DBColumnKeys.Password), PasswordPropertyText(true)]
        public string Password
        {
            get { return GetProperty<string>(); }
            set
            {
                if (value == null || value.Length == 40)
                {
                    SetProperty(value);
                    return;
                }
                var rez = ValidateText(this, value, false);
                if (rez.Length > 0)
                    throw new ArgumentException(rez);
                SetProperty(GetSha(value));
            }
        }

        #region IComparable Members

        public new int CompareTo(object obj)
        {
            return base.CompareTo(obj);// this.ToString().CompareTo(obj.ToString());
        }

        #endregion

        [Browsable(false)]
        public bool IsCurrent
        {
            get { return this == CurrentUser; }
        }

        [Browsable(false)]
        public string Token { get; set; }

        public string AuthenticationType { get; set; }

        public bool IsAuthenticated => string.IsNullOrEmpty(Token);

        public string NameRU
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string NameEN
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public override void Dispose()
        {
            base.Dispose();
        }


    }

    [Flags]
    public enum UserPasswordSpec
    {
        None = 0,
        CharNumbers = 2,
        CharUppercase = 4,
        CharLowercase = 8,
        CharSpecial = 16,
        CharRepet = 32,
        Login = 64,
        Lenght6 = 128,
        Lenght8 = 256,
        Lenght10 = 512,
    }

    public static class UserExtension
    {
        public static IEnumerable<User> GetUsers(this DBItem item, DBItem filter = null)
        {
            foreach (var access in item.Access.Items)
            {
                if (access.Create && (filter == null || filter.Access.Get(access.Group).Edit))
                {
                    foreach (User user in User.DBTable)
                    {
                        if (user.Access.Get(access.Group).Create)
                            yield return user;
                    }
                }
            }
        }

        public static IEnumerable<Position> GetPositions(this DBItem item, DBItem filter = null)
        {
            foreach (var access in item.Access.Items)
            {
                if (access.Create && (filter == null || filter.Access.Get(access.Group).Edit))
                {
                    foreach (Position position in Position.DBTable)
                    {
                        if (position.Access.Get(access.Group).Create)
                            yield return position;
                    }
                }
            }
        }

        public static IEnumerable<Department> GetDepartment(this DBItem item, DBItem filter = null)
        {
            foreach (var access in item.Access.Items)
            {
                if (access.Create && (filter == null || filter.Access.Get(access.Group).Edit))
                {
                    foreach (Department department in Department.DBTable)
                    {
                        if (department.Access.Get(access.Group).Create)
                            yield return department;
                    }
                }
            }
        }
    }
}
