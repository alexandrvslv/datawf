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
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{
    [Table("duser_application", "User")]
    public class UserApplication : DBItem
    {
        public static readonly DBTable<UserApplication> DBTable = GetTable<UserApplication>();
        public static readonly DBColumn EMailKey = DBTable.ParseProperty(nameof(EMail));
        public static readonly DBColumn EmailVerifiedKey = DBTable.ParseProperty(nameof(EmailVerified));
        public static readonly DBColumn PhoneKey = DBTable.ParseProperty(nameof(Phone));
        public static readonly DBColumn PhoneVerifiedKey = DBTable.ParseProperty(nameof(PhoneVerified));
        public static readonly DBColumn FirstNameKey = DBTable.ParseProperty(nameof(FirstName));
        public static readonly DBColumn LastNameKey = DBTable.ParseProperty(nameof(LastName));
        public static readonly DBColumn MiddleNameKey = DBTable.ParseProperty(nameof(MiddleName));
        public static readonly DBColumn CompanyKey = DBTable.ParseProperty(nameof(Company));
        public static readonly DBColumn DepartmentKey = DBTable.ParseProperty(nameof(Department));
        public static readonly DBColumn PositionKey = DBTable.ParseProperty(nameof(Position));
        public static readonly DBColumn UserKey = DBTable.ParseProperty(nameof(UserId));
        public static event Func<UserApplication, Task> ApplicationCreated;
        public static event Func<UserApplication, Task> ApplicationAccepted;
        public static event Func<UserApplication, Task> ApplicationEmailVerified;
        private User user;

        public UserApplication()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("email", 1024, Keys = DBColumnKeys.Code | DBColumnKeys.Notnull)]//Index("duser_request_email", true)
        public string EMail
        {
            get => GetValue<string>(EMailKey);
            set => SetValue(value?.Trim(), EMailKey);
        }

        [Column("email_verified", Keys = DBColumnKeys.System), DefaultValue(false)]
        public bool? EmailVerified
        {
            get => GetValue<bool?>(EmailVerifiedKey);
            set => SetValue(value, EmailVerifiedKey);
        }

        [Column("phone", 30)]
        public string Phone
        {
            get => GetValue<string>(PhoneKey);
            set => SetValue(value?.Trim(), PhoneKey);
        }

        [Column("phone_verified", Keys = DBColumnKeys.System), DefaultValue(false)]
        public bool? PhoneVerified
        {
            get => GetValue<bool?>(PhoneVerifiedKey);
            set => SetValue(value, PhoneVerifiedKey);
        }

        [Column("name_first", 50, Keys = DBColumnKeys.Notnull)]
        public string FirstName
        {
            get => GetValue<string>(FirstNameKey);
            set => SetValue(value?.Trim(), FirstNameKey);
        }

        [Column("name_last", 50, Keys = DBColumnKeys.Notnull)]
        public string LastName
        {
            get => GetValue<string>(LastNameKey);
            set => SetValue(value?.Trim(), LastNameKey);
        }

        [Column("name_middle", 50)]
        public string MiddleName
        {
            get => GetValue<string>(MiddleNameKey);
            set => SetValue(value?.Trim(), MiddleNameKey);
        }

        [Column("company", 100, Keys = DBColumnKeys.Notnull)]
        public string Company
        {
            get => GetValue<string>(CompanyKey);
            set => SetValue(value?.Trim(), CompanyKey);
        }

        [Column("department", 100, Keys = DBColumnKeys.Notnull)]
        public string Department
        {
            get => GetValue<string>(DepartmentKey);
            set => SetValue(value?.Trim(), DepartmentKey);
        }

        [Column("position", 100, Keys = DBColumnKeys.Notnull)]
        public string Position
        {
            get => GetValue<string>(PositionKey);
            set => SetValue(value?.Trim(), PositionKey);
        }

        [Column("user_id", Keys = DBColumnKeys.System)]
        public int? UserId
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

        [ControllerMethod]
        public async Task<UserApplication> Approve(DBTransaction transaction)
        {
            if (User != null)
            {
                throw new InvalidOperationException($"User already exist!");
            }
            if(Status == DBStatus.Error)
            {
                throw new InvalidOperationException($"Application is Rejected!");
            }
            //if (EmailVerified == false)
            //{
            //    throw new InvalidOperationException($"Email Verification not Passed!");
            //}
            var (company, department, position) = CheckValues(transaction);
            var user = new User
            {
                EMail = EMail,
                Login = EMail.Substring(0, EMail.IndexOf('@')),
                Phone = Phone,
                NameEN = $"{LastName} {FirstName}",
                Company = company,
                Department = department,
                Position = position,
            };
            await User.Save(transaction);
            var check = User.DBTable.Select(User.DBTable.CodeKey, CompareType.Equal, user.Login).ToList();
            if (check.Count > 1)
            {
                user.Login = $"{user.Login}.{Helper.IntToChar(check.Count).ToLowerInvariant()}";
            }
            User = user;
            Status = DBStatus.Archive;
            await Save(transaction);
            return this;
        }

        [ControllerMethod]
        public async Task<UserApplication> Reject(DBTransaction transaction)
        {
            Status = DBStatus.Error;
            await Save(transaction);
            return this;
        }

        [ControllerMethod(true)]
        public async Task<bool> EmailVerification(DBTransaction transaction)
        {
            EmailVerified = true;
            Status = DBStatus.Actual;
            await Save(transaction);
            return true;
        }

        protected override Task<bool> OnSaving(DBTransaction transaction)
        {
            if ((UpdateState & DBUpdateState.Insert) != 0)
            {
                CheckValues(transaction);
            }
            return base.OnSaving(transaction);
        }

        public override void OnAccepting(IUserIdentity user)
        {
            if ((UpdateState & DBUpdateState.Insert) != 0)
            {
                _ = OnApplicationCreated();
            }
            else if (EmailVerified == true && IsChangedKey(EmailVerifiedKey))
            {
                _ = OnApplicationEmailVerified();
            }
            else if (User != null && IsChangedKey(UserKey))
            {
                _ = OnApplicationAccepted();
            }
        }

        private Task OnApplicationEmailVerified()
        {
            return ApplicationEmailVerified?.Invoke(this);
        }

        private Task OnApplicationAccepted()
        {
            return ApplicationAccepted?.Invoke(this);
        }

        private Task OnApplicationCreated()
        {
            return ApplicationCreated?.Invoke(this);
        }

        private (Company company, Department department, Position position) CheckValues(DBTransaction transaction)
        {
            Counterpart.Company.VTTable.LoadCache("", DBLoadParam.Referencing, transaction);
            Common.Department.DBTable.LoadCache("", DBLoadParam.Referencing, transaction);
            Common.Position.DBTable.LoadCache("", DBLoadParam.Referencing, transaction);
            Common.User.DBTable.LoadCache("", DBLoadParam.Referencing, transaction);

            if (Common.User.GetByEmail(EMail) != null)
            {
                throw new ArgumentException($"User with specified email: {EMail} already exist!", nameof(EMail));
            }

            var company = (Company)null;
            using (var queryCompany = new QQuery(Counterpart.Company.VTTable))
            {
                queryCompany.BuildNameParam(nameof(Customer.ShortName), CompareType.Equal, $"{Company}");
                queryCompany.BuildNameParam(nameof(Customer.Name), CompareType.Equal, $"{Company}").Logic = LogicType.Or;
                company = Counterpart.Company.VTTable.Select(queryCompany).FirstOrDefault();
            }
            if (company == null)
            {
                throw new ArgumentException($"Company with specified name: {Company} not found!", nameof(Company));
            }

            var department = (Department)null;
            using (var queryDepartment = new QQuery(Common.Department.DBTable))
            {
                queryDepartment.BuildParam(Common.Department.CompanyKey, company);
                queryDepartment.BuildNameParam(nameof(Common.Department.Name), CompareType.Equal, $"{Department}");
                department = Common.Department.DBTable.Select(queryDepartment).FirstOrDefault();
            }
            if (department == null)
            {
                throw new ArgumentException($"Department with specified name: {Department} not found!", nameof(Department));
            }

            var position = (Position)null;
            using (var queryPosition = new QQuery(Common.Position.DBTable))
            {
                queryPosition.BuildParam(Common.Position.DepartmentKey, department);
                queryPosition.BuildNameParam(nameof(Common.Position.Name), CompareType.Equal, $"{Position}");
                position = Common.Position.DBTable.Select(queryPosition).FirstOrDefault();
            }
            if (position == null)
            {
                throw new ArgumentException($"Position with specified name: {Position} not found!", nameof(Position));
            }
            return (company, department, position);
        }
    }
}
