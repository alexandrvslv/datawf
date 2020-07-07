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
        public static readonly DBColumn TypeKey = DBTable.ParseProperty(nameof(Type));
        public static readonly DBColumn EMailKey = DBTable.ParseProperty(nameof(EMail));
        public static readonly DBColumn PasswordKey = DBTable.ParseProperty(nameof(Password));
        public static readonly DBColumn TemporaryPasswordKey = DBTable.ParseProperty(nameof(TemporaryPassword));
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
        public static Func<UserApplication, Task> Registered;
        public static Func<UserApplication, Task> Approved;
        public static Func<UserApplication, Task> Rejected;
        public static Func<UserApplication, Task> Verified;
        private User user;

        public UserApplication()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("app_type", 1024, Keys = DBColumnKeys.ElementType | DBColumnKeys.Notnull)]
        public UserApplicationType? Type
        {
            get => GetValue<UserApplicationType?>(TypeKey);
            set => SetValue(value, TypeKey);
        }

        [Column("email", 1024, Keys = DBColumnKeys.Code | DBColumnKeys.Notnull)]//Index("duser_request_email", true)
        public string EMail
        {
            get => GetValue<string>(EMailKey);
            set => SetValue(value?.Trim(), EMailKey);
        }

        [Column("password", Keys = DBColumnKeys.Notnull)]//Index("duser_request_email", true)
        public string Password
        {
            get => GetValue<string>(PasswordKey);
            set => SetValue(value, PasswordKey);
        }

        [Column("temp_password")]//Index("duser_request_email", true)
        public string TemporaryPassword
        {
            get => GetValue<string>(TemporaryPasswordKey);
            set => SetValue(value, TemporaryPasswordKey);
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

        [ControllerMethod(Anonymous = true)]
        public static async Task<UserApplication> Register([ControllerParameter(ControllerParameterType.Body)] UserApplication application)
        {
            if (Common.User.GetByEmail(application.EMail) != null)
            {
                throw new ArgumentException($"User with specified email: {application.EMail} already exist!", nameof(EMail));
            }

            using (var query = new QQuery(DBTable))
            {
                query.BuildParam(EMailKey, application.EMail);
                query.BuildParam(TypeKey, (int)application.Type);
                query.BuildParam(DBTable.StatusKey, CompareType.In, new[] { (int)DBStatus.Actual, (int)DBStatus.New });
                var exist = DBTable.Load(query);
                if (exist.Count() > 0)
                {
                    throw new ArgumentException($"Application with specified email: {application.EMail} already in process!", nameof(EMail));
                }
            }
            await application.Save((IUserIdentity)null);
            _ = application.OnRegistered();
            return application;
        }

        [ControllerMethod]
        public async Task<UserApplication> Approve(DBTransaction transaction)
        {
            var caller = transaction.Caller as User;
            if (caller == null || User.DBTable.Access.GetFlag(AccessType.Admin, caller))
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
                var user = new User
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
                var check = User.DBTable.Select(User.DBTable.CodeKey, CompareType.Equal, user.Login).ToList();
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
                var user = User.GetByEmail(EMail);
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
                user.IsTemporaryPassword = true;
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

        private Task OnRegistered()
        {
            return Registered?.Invoke(this);
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

    public enum UserApplicationType
    {
        NewAccount,
        ResetPassword
    }
}
