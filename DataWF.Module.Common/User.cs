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

namespace DataWF.Module.Common
{

    public enum UserTypes
    {
        Persone,
        Department,
        Division
    }

    [Table("flow", "ruser", BlockSize = 100)]
    public class User : DBItem, IComparable, IDisposable
    {
        public static User CurrentUser { get; set; }

        public static DBTable<User> DBTable
        {
            get { return DBService.GetTable<User>(); }
        }

        [NonSerialized]
        protected bool online = false;

        public User()
        {
            Build(DBTable);
            UserType = UserTypes.Persone;
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { this[Table.PrimaryKey] = value; }
        }

        [Column("parentid", Keys = DBColumnKeys.Group), Index("ruser_parentid"), Browsable(false)]
        public int? ParentId
        {
            get { return GetValue<int?>(Table.GroupKey); }
            set { this[Table.GroupKey] = value; }
        }

        [Reference("fk_ruser_parentid", "ParentId")]
        public User Parent
        {
            get { return GetReference<User>(Table.GroupKey); }
            set { SetReference(value, Table.GroupKey); }
        }

        [Column("typeid", Keys = DBColumnKeys.Type)]
        public UserTypes? UserType
        {
            get { return (UserTypes?)GetValue<int?>(Table.TypeKey); }
            set { this[Table.TypeKey] = (int?)value; }
        }

        [Column("login", 40, Keys = DBColumnKeys.Code | DBColumnKeys.View | DBColumnKeys.Indexing), Index("ruser_login", true)]
        public string Login
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { this[Table.CodeKey] = value; }
        }

        [Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public override string Name
        {
            get { return GetName("name"); }
            set { SetName("name", value); }
        }

        [Column("positionid")]
        public int? PositionId
        {
            get { return GetProperty<int?>(nameof(PositionId)); }
            set { SetProperty(value, nameof(PositionId)); }
        }

        [Reference("fk_ruser_positionid", nameof(PositionId))]
        public Position Position
        {
            get { return GetPropertyReference<Position>(nameof(PositionId)); }
            set { SetPropertyReference(value, nameof(PositionId)); }
        }

        [ReadOnly(true)]
        [Column("super")]
        public bool? Super
        {
            get { return GetValue<bool?>(ParseProperty(nameof(Super))); }
            set { SetProperty(value, nameof(Super)); }
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

        [Column("email")]
        public string EMail
        {
            get { return GetProperty<string>(nameof(EMail)); }
            set { SetProperty(value, nameof(EMail)); }
        }

        [Column("networkid", 2048)]
        public string NetworkId
        {
            get { return GetProperty<string>(nameof(NetworkId)); }
            set { SetProperty(value, nameof(NetworkId)); }
        }

        public bool IsBlock
        {
            get { return Status != DBStatus.Actual; }
            set { Status = value ? DBStatus.Actual : DBStatus.Error; }
        }

        [Column("password", Keys = DBColumnKeys.Password), PasswordPropertyText(true)]
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

        private static string GetString(byte[] data)
        {
            var s = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                s.Append(data[i].ToString("x2"));

            return s.ToString();
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

            DBProcedure proc = User.Table.Schema?.Procedures["simple"];
            if (proc != null)
            {
                string[] split = proc.Source.Split('\r', '\n');
                foreach (string s in split)
                    if (s.Length > 0 && password.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        message += Locale.Get("Login", " Should not contain simple words.");
                        break;
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

        public static IEnumerable<DataLog> GetOld(User User)
        {
            QQuery filter = new QQuery(string.Empty, DataLog.DBTable);
            filter.BuildPropertyParam(nameof(DataLog.UserId), CompareType.Equal, User.PrimaryId);
            filter.BuildPropertyParam(nameof(DataLog.LogType), CompareType.Equal, DataLogType.Password);

            var plist = DataLog.DBTable.Load(filter, DBLoadParam.Load | DBLoadParam.Synchronize);
            plist.Sort(new DBComparer(DataLog.DBTable.PrimaryKey, ListSortDirection.Descending));

            return plist;
        }

        public static User GetUser(string login, string passoword)
        {
            var query = new QQuery(string.Empty, User.DBTable);
            query.BuildPropertyParam(nameof(User.Login), CompareType.Equal, login);
            query.BuildPropertyParam(nameof(User.Password), CompareType.Equal, passoword);

            return User.DBTable.Select(query).FirstOrDefault();
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


        public IEnumerable<User> GetUsers()
        {
            return DBTable.Select(Table.GroupKey, PrimaryId, CompareType.Equal);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
