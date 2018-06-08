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

namespace DataWF.Module.Common
{
    [DataContract, Table("ruser", "User", BlockSize = 100)]
    public class User : DBItem, IComparable, IDisposable
    {
        internal static void SetCurrent()
        {
            var user = DBTable.LoadByCode(Environment.UserName);
            if (user != null)
                UserLog.LogUser(user, UserLogType.Authorization, "GetUser");
            CurrentUser = user;
        }

        public static void SetCurrent(string login, string password)
        {
            var user = GetUser(login, GetSha(password));
            CurrentUser = user ?? throw new Exception();
        }

        public static User GetByNetId(string netid)
        {
            return DBTable.Select($"{User.DBTable.ParseProperty(nameof(User.NetworkAddress)).Name} like '%{netid}%'").FirstOrDefault();
        }

        public static List<User> LoadADUsers(string userName, string password)
        {
            var users = new List<User>();
            try
            {
                var domain = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
                var domain1 = domain.Substring(0, domain.IndexOf('.'));
                var domain2 = domain.Substring(domain.IndexOf('.') + 1);
                var ldapDom = $"dc={domain1},dc={domain2}";
                var userDN = $"{userName},{ldapDom}";//$"cn={userName},o={domain1}";
                var attributes = new string[] { "cn", "company", "lastLongon", "lastLongoff", "mail", "mailNickname", "name", "title", "userPrincipalName" };
                using (var conn = new LdapConnection())
                {
                    conn.Connect(domain, LdapConnection.DEFAULT_PORT);
                    conn.Bind(userDN, password);
                    var results = conn.Search(ldapDom, //search base
                        LdapConnection.SCOPE_SUB, //scope 
                        "(&(objectCategory=person)(objectClass=user)(!(userAccountControl:1.2.840.113556.1.4.803:=2)))", //filter
                        attributes, //attributes 
                        false); //types only 
                    while (results.HasMore())
                    {
                        try
                        {

                            var resultRecord = results.Next();
                            var attribute = resultRecord.getAttribute("mailNickname");
                            if (attribute != null)
                            {
                                Position position = null;
                                var positionName = resultRecord.getAttribute("title")?.StringValue;
                                if (!string.IsNullOrEmpty(positionName))
                                {
                                    position = Position.DBTable.LoadByCode(positionName);
                                    if (position == null)
                                    {
                                        position = new Position();
                                    }
                                    position.Code = positionName;
                                    position.Name = positionName;
                                    position.Save();
                                }

                                var user = User.DBTable.LoadByCode(attribute.StringValue, User.DBTable.ParseProperty(nameof(User.Login)), DBLoadParam.None);
                                if (user == null)
                                {
                                    user = new User();
                                }
                                user.Position = position;
                                user.Login = attribute.StringValue;
                                user.EMail = resultRecord.getAttribute("mail")?.StringValue;
                                user.Name = resultRecord.getAttribute("name")?.StringValue;
                                user.Save();
                            }

                        }
                        catch (LdapException e)
                        {
                            Debug.WriteLine(e.Message);
                        }
                    }
                    conn.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
            }
            return users;
        }

        public static User CurrentUser { get; internal set; }

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

        public static string ValidateText(User User, string password, bool checkOld)
        {
            string message = string.Empty;
            if (password.Length < 8)//(?=^.{6,255}$)
                message += Locale.Get("Login", " Must be more than 8 characters long.");
            if (!Regex.IsMatch(password, @"(?=.*\d)(?=.*[A-Z])(?=.*[a-z])(?=.*[^A-Za-z0-9])^.*", RegexOptions.CultureInvariant))
                message += Locale.Get("Login", " Should contain a number, uppercase and lowercase letters and special character.");
            if (Regex.IsMatch(password, @"(.)\1{2,}", RegexOptions.CultureInvariant))
                message += Locale.Get("Login", " Remove repeted characters.");
            if (User.Login != null && password.IndexOf(User.Login, StringComparison.OrdinalIgnoreCase) >= 0)
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

        public static void SetCurrentUser(User user)
        {
            CurrentUser = user;
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
            set { this[Table.PrimaryKey] = value; }
        }

        [DataMember, Column("login", 256, Keys = DBColumnKeys.Code | DBColumnKeys.Indexing), Index("ruser_login", true)]
        public string Login
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { this[Table.CodeKey] = value; }
        }

        [DataMember, Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public override string Name
        {
            get { return GetName(nameof(Name)); }
            set { SetName(nameof(Name), value); }
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
            set { SetPropertyReference(value); }
        }

        [ReadOnly(true)]
        [DataMember, Column("super", Default = "false", Keys = DBColumnKeys.Notnull)]
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

        [DataMember, Column("network_address", 2048)]
        public string NetworkAddress
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public bool IsBlock
        {
            get { return Status != DBStatus.Actual; }
            set { Status = value ? DBStatus.Actual : DBStatus.Error; }
        }

        [DataMember, Column("password", 256, Keys = DBColumnKeys.Password), PasswordPropertyText(true)]
        public string Password
        {
            get { return GetProperty<string>(nameof(Password)); }
            set
            {
                var rez = ValidateText(this, value, false);
                if (rez.Length > 0)
                    throw new ArgumentException(rez);
                SetProperty(GetSha(value), nameof(Password));
            }
        }

        #region IComparable Members

        public new int CompareTo(object obj)
        {
            return base.CompareTo(obj);// this.ToString().CompareTo(obj.ToString());
        }

        #endregion

        public bool IsCurrent
        {
            get { return this == CurrentUser; }
        }

        public override void Dispose()
        {
            base.Dispose();
        }


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
