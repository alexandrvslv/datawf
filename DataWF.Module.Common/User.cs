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
using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
using MailKit.Net.Smtp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{
    [DataContract, Table("ruser", "User", BlockSize = 100)]
    public class User : DBItem, IComparable, IDisposable, IUserIdentity
    {
        private static DBColumn abbreviationKey = DBColumn.EmptyKey;
        private static DBColumn departmentKey = DBColumn.EmptyKey;
        private static DBColumn positionKey = DBColumn.EmptyKey;
        private static DBColumn emailKey = DBColumn.EmptyKey;
        private static DBColumn phoneKey = DBColumn.EmptyKey;
        private static DBColumn passwordKey = DBColumn.EmptyKey;
        private static DBColumn superKey = DBColumn.EmptyKey;
        private static DBColumn refreshTokenKey = DBColumn.EmptyKey;
        private static DBColumn nameENKey = DBColumn.EmptyKey;
        private static DBColumn nameRUKey = DBColumn.EmptyKey;
        private static DBColumn companyKey = DBColumn.EmptyKey;
        private static DBColumn authTypeKey = DBColumn.EmptyKey;
        private static DBTable<User> dbTable;

        public static DBColumn AbbreviationKey => DBTable.ParseProperty(nameof(Abbreviation), ref abbreviationKey);
        public static DBColumn DepartmentKey => DBTable.ParseProperty(nameof(DepartmentId), ref departmentKey);
        public static DBColumn PositionKey => DBTable.ParseProperty(nameof(PositionId), ref positionKey);
        public static DBColumn EmailKey => DBTable.ParseProperty(nameof(EMail), ref emailKey);
        public static DBColumn PhoneKey => DBTable.ParseProperty(nameof(Phone), ref phoneKey);
        public static DBColumn PasswordKey => DBTable.ParseProperty(nameof(Password), ref passwordKey);
        public static DBColumn SuperKey => DBTable.ParseProperty(nameof(Super), ref superKey);
        public static DBColumn RefreshTokenKey => DBTable.ParseProperty(nameof(RefreshToken), ref refreshTokenKey);
        public static DBColumn NameENKey => DBTable.ParseProperty(nameof(NameEN), ref nameENKey);
        public static DBColumn NameRUKey => DBTable.ParseProperty(nameof(NameRU), ref nameRUKey);
        public static DBColumn CompanyKey => DBTable.ParseProperty(nameof(Company), ref companyKey);
        public static DBColumn AuthTokenKey => DBTable.ParseProperty(nameof(AuthType), ref authTypeKey);

        public static DBTable<User> DBTable => dbTable ?? (dbTable = GetTable<User>());
        private static SmtpSetting config;

        public static User GetByEmail(string email)
        {
            return DBTable.SelectOne(EmailKey, email);
        }

        public static User GetByLogin(string login)
        {
            return DBTable.SelectOne(DBTable.CodeKey, login);
        }

        public static User GetByEnvironment()
        {
            return DBTable.LoadByCode(Environment.UserName);
        }

        public static async Task StartSession(User value)
        {
            if (value != null)
            {
                if (value.LogStart == null)
                {
                    await UserLog.LogUser(value, UserLogType.Authorization, "GetUser");
                }
            }
        }

        public static async Task<User> StartSession(string login, string password)
        {
            var user = await GetUser(login, Helper.GetSha512(password));
            await StartSession(user ?? throw new KeyNotFoundException("User not found!"));
            return user;
        }

        public static Task<User> StartSession(string email, SecureString password)
        {
            return StartSession(new NetworkCredential(email, password));
        }

        public static async Task<User> StartSession(string email)
        {
            var user = GetByEmail(email);
            if (user == null)
                throw new KeyNotFoundException("User not found!");
            await StartSession(user);
            return user;
        }

        public static void ChangePassword(User user, string password)
        {
            if (config == null)
            {
                config = SmtpSetting.Load();
            }
            user.Password = Helper.Decript(password, config.PassKey);
        }

        public static async Task<User> StartSession(NetworkCredential credentials)
        {
            if (config == null)
            {
                config = SmtpSetting.Load();
            }
            credentials.Password = Helper.Decript(credentials.Password, config.PassKey);
            var user = GetByEmail(credentials.UserName);
            if (user == null)
            {
                user = GetByLogin(credentials.UserName);
            }

            if (user == null || user.Status == DBStatus.Archive || user.Status == DBStatus.Error)
            {
                throw new KeyNotFoundException("User not found!");
            }

            if (user.AuthType == UserAuthType.SMTP)
            {
                using (var smtpClient = new SmtpClient { Timeout = 20000 })
                {
                    smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    smtpClient.Connect(config.Host, config.Port, config.SSL);
                    smtpClient.Authenticate(credentials);
                }
            }
            else if (user.AuthType == UserAuthType.LDAP)
            {
                var address = new System.Net.Mail.MailAddress(user.EMail);
                var domain = address.Host.Substring(0, address.Host.IndexOf('.'));
                if (!LdapHelper.ValidateUser(domain, address.User, credentials.Password))
                {
                    throw new Exception("Authentication fail!");
                }
            }
            else
            {
                if (!user.Password.Equals(Helper.GetSha512(credentials.Password), StringComparison.Ordinal))
                {
                    throw new Exception("Authentication fail!");
                }
            }
            await StartSession(user);

            return user;
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
                string encoded = Helper.GetSha512(password);
                foreach (var item in GetOld(User))
                {
                    if (item.TextData == encoded)
                    {
                        message += Locale.Get("Login", " Password was same before.");
                        break;
                    }
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

        public static async Task<User> GetUser(string login, string passoword)
        {
            var query = new QQuery(string.Empty, User.DBTable);
            query.BuildPropertyParam(nameof(Login), CompareType.Equal, login);
            query.BuildPropertyParam(nameof(Password), CompareType.Equal, passoword);
            var user = User.DBTable.Select(query).FirstOrDefault();
            if (user != null)
            {
                await UserLog.LogUser(user, UserLogType.Authorization, "GetUser");
            }

            return user;
        }

        protected bool online = false;
        private Company company;
        private Department department;
        private Position position;

        public User()
        { }

        public UserLog LogStart { get; set; }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get { return GetValue<int?>(CompanyKey); }
            set { SetValue(value, CompanyKey); }
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get { return GetReference(CompanyKey, ref company); }
            set { SetReference(company = value, CompanyKey); }
        }

        [DataMember, Column("login", 256, Keys = DBColumnKeys.Code | DBColumnKeys.Indexing), Index("ruser_login", true)]
        public string Login
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [DataMember, Column("abbreviation", 3, Keys = DBColumnKeys.Indexing), Index("ruser_abbreviation", true)]
        public string Abbreviation
        {
            get { return GetValue<string>(AbbreviationKey); }
            set { SetValue(value, AbbreviationKey); }
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
            get { return GetValue<int?>(DepartmentKey); }
            set { SetValue(value, DepartmentKey); }
        }


        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get { return GetReference(DepartmentKey, ref department); }
            set { SetReference(department = value, DepartmentKey); }
        }

        [DataMember, Column("position_id"), Browsable(false)]
        public int? PositionId
        {
            get { return GetValue<int?>(PositionKey); }
            set { SetValue(value, PositionKey); }
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get { return GetReference<Position>(PositionKey, ref position); }
            set
            {
                SetReference(position = value, PositionKey);
                Department = value?.Department;
            }
        }

        [ReadOnly(true)]
        [DataMember, DefaultValue(false), Column("super")]
        public bool? Super
        {
            get { return GetValue<bool?>(SuperKey); }
            set { SetValue(value, SuperKey); }
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
                OnPropertyChanged();
            }
        }

        [DataMember, Column("email", 1024, Keys = DBColumnKeys.Indexing), Index("ruser_email", true)]
        public string EMail
        {
            get { return GetValue<string>(EmailKey); }
            set { SetValue(value, EmailKey); }
        }

        [DataMember, Column("phone", 1024), Index("ruser_phone", false)]
        public string Phone
        {
            get { return GetValue<string>(PhoneKey); }
            set { SetValue(value, PhoneKey); }
        }

        public bool IsBlock
        {
            get { return Status != DBStatus.Actual; }
            set { Status = value ? DBStatus.Actual : DBStatus.Error; }
        }

        [DataMember, Column("password", 512, Keys = DBColumnKeys.Password), PasswordPropertyText(true)]
        public string Password
        {
            get { return GetValue<string>(PasswordKey); }
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
                SetValue(Helper.GetSha512(value), PasswordKey);
            }
        }

        [Browsable(false)]
        public override AccessValue Access
        {
            get
            {
                return base.Access != Table.Access ? base.Access
                  : Department?.Access ?? Position?.Access ?? Table.Access;
            }
        }

        [Browsable(false)]
        public string AccessToken { get; set; }

        [Browsable(false), DataMember, Column("token_refresh", 2048, Keys = DBColumnKeys.Password | DBColumnKeys.NoLog)]
        public string RefreshToken
        {
            get { return GetValue<string>(RefreshTokenKey); }
            set { SetValue(value, RefreshTokenKey); }
        }

        [Column("auth_type")]
        public UserAuthType? AuthType
        {
            get { return GetValue<UserAuthType?>(AuthTokenKey) ?? UserAuthType.SMTP; }
            set { SetValue(value, AuthTokenKey); }
        }

        public bool IsAuthenticated => string.IsNullOrEmpty(AccessToken);

        string IIdentity.Name => EMail;

        public string NameRU
        {
            get { return GetValue<string>(NameRUKey); }
            set { SetValue(value, NameRUKey); }
        }

        public string NameEN
        {
            get { return GetValue<string>(NameENKey); }
            set { SetValue(value, NameENKey); }
        }

        public string AuthenticationType => throw new NotImplementedException();

        public override void Dispose()
        {
            base.Dispose();
        }

        public new int CompareTo(object obj)
        {
            return base.CompareTo(obj);// this.ToString().CompareTo(obj.ToString());
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

    public enum UserAuthType
    {
        Internal = 1,
        SMTP = 2,
        LDAP = 3
    }
}
