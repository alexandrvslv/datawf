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
    public partial class UserApplication : DBItem
    {
        public static Func<UserApplication, Task> Registered;
        public static Func<UserApplication, Task> Approved;
        public static Func<UserApplication, Task> Rejected;
        public static Func<UserApplication, Task> Verified;
        private User user;

        public UserApplication(DBTable table) : base(table)
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("app_type", 1024, Keys = DBColumnKeys.ElementType | DBColumnKeys.Notnull)]
        public UserApplicationType? Type
        {
            get => GetValue<UserApplicationType?>(Table.TypeKey);
            set => SetValue(value, Table.TypeKey);
        }

        [Column("email", 1024, Keys = DBColumnKeys.Code | DBColumnKeys.Notnull)]//Index("duser_request_email", true)
        public string EMail
        {
            get => GetValue<string>(Table.EMailKey);
            set => SetValue(value?.Trim(), Table.EMailKey);
        }

        [Column("password", Keys = DBColumnKeys.Notnull)]//Index("duser_request_email", true)
        public string Password
        {
            get => GetValue<string>(Table.PasswordKey);
            set => SetValue(value, Table.PasswordKey);
        }

        [Column("temp_password")]//Index("duser_request_email", true)
        public string TemporaryPassword
        {
            get => GetValue<string>(Table.TemporaryPasswordKey);
            set => SetValue(value, Table.TemporaryPasswordKey);
        }

        [Column("email_verified", Keys = DBColumnKeys.System), DefaultValue(false)]
        public bool? EmailVerified
        {
            get => GetValue<bool?>(Table.EmailVerifiedKey);
            set => SetValue(value, Table.EmailVerifiedKey);
        }

        [Column("phone", 30)]
        public string Phone
        {
            get => GetValue<string>(Table.PhoneKey);
            set => SetValue(value?.Trim(), Table.PhoneKey);
        }

        [Column("phone_verified", Keys = DBColumnKeys.System), DefaultValue(false)]
        public bool? PhoneVerified
        {
            get => GetValue<bool?>(Table.PhoneVerifiedKey);
            set => SetValue(value, Table.PhoneVerifiedKey);
        }

        [Column("name_first", 50, Keys = DBColumnKeys.Notnull)]
        public string FirstName
        {
            get => GetValue<string>(Table.FirstNameKey);
            set => SetValue(value?.Trim(), Table.FirstNameKey);
        }

        [Column("name_last", 50, Keys = DBColumnKeys.Notnull)]
        public string LastName
        {
            get => GetValue<string>(Table.LastNameKey);
            set => SetValue(value?.Trim(), Table.LastNameKey);
        }

        [Column("name_middle", 50)]
        public string MiddleName
        {
            get => GetValue<string>(Table.MiddleNameKey);
            set => SetValue(value?.Trim(), Table.MiddleNameKey);
        }

        [Column("company", 100, Keys = DBColumnKeys.Notnull)]
        public string Company
        {
            get => GetValue<string>(Table.CompanyKey);
            set => SetValue(value?.Trim(), Table.CompanyKey);
        }

        [Column("department", 100, Keys = DBColumnKeys.Notnull)]
        public string Department
        {
            get => GetValue<string>(Table.DepartmentKey);
            set => SetValue(value?.Trim(), Table.DepartmentKey);
        }

        [Column("position", 100, Keys = DBColumnKeys.Notnull)]
        public string Position
        {
            get => GetValue<string>(Table.PositionKey);
            set => SetValue(value?.Trim(), Table.PositionKey);
        }

        [Column("user_id", Keys = DBColumnKeys.System)]
        public int? UserId
        {
            get => GetValue<int?>(Table.UserIdKey);
            set => SetValue(value, Table.UserIdKey);
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get => GetReference((DBColumn)Table.UserIdKey, ref user);
            set => SetReference(user = value, Table.UserIdKey);
        }

        [ControllerMethod]
        public async Task<UserApplication> Approve(DBTransaction transaction)
        {
            var users = (UserTable)Schema.GetTable<User>();
            var caller = transaction.Caller as User;
            if (caller == null || users.Access.GetFlag(AccessType.Admin, caller))
            {
                throw new UnauthorizedAccessException("Approving. Required administrators permission!");
            }
            if (Type == UserApplicationType.NewAccount)
            {
                if (User != null)
                {
                    throw new InvalidOperationException($"User already Registered!");
                }
                if (Status == DBStatus.Error || Status == DBStatus.Delete)
                {
                    throw new InvalidOperationException($"Application is Rejected!");
                }
                //if (EmailVerified == false)
                //{
                //    throw new InvalidOperationException($"Email Verification not Passed!");
                //}
                var (company, department, position) = CheckValues(transaction);
                var user = new User(users)
                {
                    EMail = EMail,
                    Login = EMail.Substring(0, EMail.IndexOf('@')),
                    Password = Helper.Decript(Password, SMTPSetting.Current.PassKey),
                    Phone = Phone,
                    NameEN = $"{LastName} {FirstName}",
                    Company = company,
                    Department = department,
                    Position = position,
                };
                var check = users.Select(users.CodeKey, CompareType.Equal, user.Login).ToList();
                if (check.Count > 1)
                {
                    user.Login = $"{user.Login}.{Helper.IntToChar(check.Count).ToLowerInvariant()}";
                }
                await user.Save(transaction);
                User = user;
                Status = DBStatus.Archive;
                await Save(transaction);
            }
            else if (Type == UserApplicationType.ResetPassword)
            {
                if (Status == DBStatus.Archive)
                {
                    throw new InvalidOperationException($"Application is Closed!");
                }
                if (Status == DBStatus.Error || Status == DBStatus.Delete)
                {
                    throw new InvalidOperationException($"Application is Rejected!");
                }
                var user = users.GetByEmail(EMail);
                if (user == null)
                {
                    throw new InvalidOperationException($"Application is Closed!");
                }
                var (company, department, position) = CheckValues(transaction);
                if (user.Company != company)
                {
                    throw new InvalidOperationException($"Wrong Company spefied in Application!");
                }
                if (user.Department != department)
                {
                    throw new InvalidOperationException($"Wrong Department spefied in Application!");
                }
                if (user.Position != position)
                {
                    throw new InvalidOperationException($"Wrong Position spefied in Application!");
                }
                TemporaryPassword = GeneratePassword();

                user.Password = TemporaryPassword;
                user.IsTempPassword = true;
                await user.Save(transaction);

                User = user;
                Status = DBStatus.Archive;
                await Save(transaction);
            }
            _ = OnAccepted();
            return this;
        }

        private string GeneratePassword()
        {
            return "Test123!";
        }

        [ControllerMethod]
        public async Task<UserApplication> Reject(DBTransaction transaction)
        {
            Status = DBStatus.Error;
            await Save(transaction);
            _ = OnRejected();
            return this;
        }

        [ControllerMethod(Anonymous = true, ReturnHtml = true)]
        public async Task<string> EmailVerification(DBTransaction transaction)
        {
            EmailVerified = true;
            Status = DBStatus.Actual;
            await Save(transaction);
            _ = OnVerified();
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset = ""utf-8""/>
    <title>Email Verification</title>
 </head>
 <body>
     <H1>Email Verification Competed Successfully</H1>
 </body>
 </html> ";
        }

        protected override Task<bool> OnSaving(DBTransaction transaction)
        {
            if ((UpdateState & DBUpdateState.Insert) != 0)
            {
                //CheckValues(transaction);
            }
            return base.OnSaving(transaction);
        }

        private Task OnVerified()
        {
            return Verified?.Invoke(this);
        }

        private Task OnAccepted()
        {
            return Approved?.Invoke(this);
        }

        private Task OnRejected()
        {
            return Rejected?.Invoke(this);
        }

        public Task OnRegistered()
        {
            return Registered?.Invoke(this);
        }

        private (Company company, Department department, Position position) CheckValues(DBTransaction transaction)
        {
            var schema = Schema;
            var companies = (CompanyTable)schema.GetTable<Counterpart.Company>();
            var departments = (DepartmentTable)schema.GetTable<Common.Department>();
            var positions = (PositionTable)schema.GetTable<Common.Position>();
            var users = (UserTable)schema.GetTable<Common.User>();

            companies.LoadCache("", DBLoadParam.Referencing, transaction);
            departments.LoadCache("", DBLoadParam.Referencing, transaction);
            positions.LoadCache("", DBLoadParam.Referencing, transaction);
            users.LoadCache("", DBLoadParam.Referencing, transaction);

            //var userTable = 

            if (users.GetByEmail(EMail) != null)
            {
                throw new ArgumentException($"User with specified email: {EMail} already exist!", nameof(EMail));
            }

            var company = (Company)null;
            using (var queryCompany = new QQuery(companies))
            {
                queryCompany.BuildNameParam(nameof(Customer.ShortName), CompareType.Equal, $"{Company}");
                queryCompany.BuildNameParam(nameof(Customer.Name), CompareType.Equal, $"{Company}").Logic = LogicType.Or;
                company = companies.Select(queryCompany).FirstOrDefault();
            }
            if (company == null)
            {
                throw new ArgumentException($"Company with specified name: {Company} not found!", nameof(Company));
            }

            var department = (Department)null;
            using (var queryDepartment = new QQuery(departments))
            {
                queryDepartment.BuildParam(departments.CompanyIdKey, company);
                queryDepartment.BuildNameParam(nameof(Common.Department.Name), CompareType.Equal, $"{Department}");
                department = departments.Select(queryDepartment).FirstOrDefault();
            }
            if (department == null)
            {
                throw new ArgumentException($"Department with specified name: {Department} not found!", nameof(Department));
            }

            var position = (Position)null;
            using (var queryPosition = new QQuery(positions))
            {
                queryPosition.BuildParam(positions.DepartmentIdKey, department);
                queryPosition.BuildNameParam(nameof(Common.Position.Name), CompareType.Equal, $"{Position}");
                position = positions.Select(queryPosition).FirstOrDefault();
            }
            if (position == null)
            {
                throw new ArgumentException($"Position with specified name: {Position} not found!", nameof(Position));
            }
            return (company, department, position);
        }
    }

    public enum UserApplicationType
    {
        NewAccount,
        ResetPassword
    }
}
