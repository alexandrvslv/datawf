using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
using MailKit.Net.Smtp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{
    [Table("ruser", "User", BlockSize = 100)]
    public class User : DBUser, IComparable, IDisposable
    {
        public static readonly DBTable<User> DBTable = GetTable<User>();
        public static readonly DBColumn AbbreviationKey = DBTable.ParseProperty(nameof(Abbreviation));
        public static readonly DBColumn DepartmentKey = DBTable.ParseProperty(nameof(DepartmentId));
        public static readonly DBColumn PositionKey = DBTable.ParseProperty(nameof(PositionId));
        public static readonly DBColumn EmailKey = DBTable.ParseProperty(nameof(EMail));
        public static readonly DBColumn PhoneKey = DBTable.ParseProperty(nameof(Phone));
        public static readonly DBColumn PasswordKey = DBTable.ParseProperty(nameof(Password));
        public static readonly DBColumn IsTemporaryPasswordKey = DBTable.ParseProperty(nameof(IsTemporaryPassword));
        public static readonly DBColumn SuperKey = DBTable.ParseProperty(nameof(Super));
        public static readonly DBColumn RefreshTokenKey = DBTable.ParseProperty(nameof(RefreshToken));
        public static readonly DBColumn NameENKey = DBTable.ParseProperty(nameof(NameEN));
        public static readonly DBColumn NameRUKey = DBTable.ParseProperty(nameof(NameRU));
        public static readonly DBColumn CompanyKey = DBTable.ParseProperty(nameof(Company));
        public static readonly DBColumn AuthTokenKey = DBTable.ParseProperty(nameof(AuthType));
        public static readonly DBColumn AddressIdKey = DBTable.ParseProperty(nameof(AddessId));
        public static readonly DBColumn ExternalIdKey = DBTable.ParseProperty(nameof(ExternalId));
        private static readonly PasswordSpec PasswordSpecification = PasswordSpec.Lenght6 | PasswordSpec.CharSpecial | PasswordSpec.CharNumbers;

        public const string AuthenticationScheme = "Bearer";

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

        public static async Task RegisterSession(User user, LoginModel login = null)
        {
            if (user == null || user.LogStart != null)
            {
                return;
            }
            var text = login == null ? $"Login:{user.EMail}" : $"Login:{user.EMail}\nPlatform:{login.Platform}\nApp:{login.Application}\nVersion:{login.Version}";
            await UserReg.LogUser(user, UserRegType.Authorization, text);
        }

        public static Task<User> StartSession(string login, string password)
        {
            return StartSession(new LoginModel { Email = login, Password = password, Platform = "unknown", Application = "unknown", Version = "1.0.0.0" });
        }

        public static async Task<User> StartSession(string email)
        {
            var user = GetByEmail(email) ?? GetByLogin(email);
            if (user == null || user.Status == DBStatus.Archive || user.Status == DBStatus.Error)
            {
                throw new KeyNotFoundException("User not found!");
            }

            await RegisterSession(user);
            return user;
        }

        public static async Task<User> StartSession(LoginModel login)
        {
            var user = GetByEmail(login.Email) ?? GetByLogin(login.Email);
            if (user == null || user.Status == DBStatus.Archive || user.Status == DBStatus.Error)
            {
                throw new KeyNotFoundException("User not found!");
            }
            var password = SMTPSetting.Current == null ? login.Password : Helper.Decript(login.Password, SMTPSetting.Current.PassKey);

            if (user.AuthType == UserAuthType.SMTP)
            {
                using (var smtpClient = new SmtpClient { Timeout = 20000 })
                {
                    smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    smtpClient.Connect(SMTPSetting.Current.Host, SMTPSetting.Current.Port, SMTPSetting.Current.SSL);
                    smtpClient.Authenticate(user.EMail, password);
                }
            }
            else if (user.AuthType == UserAuthType.LDAP)
            {
                var address = new System.Net.Mail.MailAddress(user.EMail);
                var domain = address.Host.Substring(0, address.Host.IndexOf('.'));
                if (!LdapHelper.ValidateUser(domain, address.User, password))
                {
                    throw new Exception("Authentication fail!");
                }
            }
            else
            {
                if (!user.Password.Equals(Helper.GetSha512(password), StringComparison.Ordinal))
                {
                    throw new Exception("Authentication fail!");
                }
            }
            await RegisterSession(user, login);

            return user;
        }

        public static string ValidateText(User user, string password)
        {
            string message = Helper.PasswordVerification(password, user.Login, PasswordSpecification);
            var proc = User.DBTable.Schema?.Procedures["simple"];
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

            if (PasswordSpecification.HasFlag(PasswordSpec.CheckOld))
            {
                string encoded = Helper.GetSha512(password);
                foreach (var item in GetOld(user))
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

        public static IEnumerable<UserReg> GetOld(User User)
        {
            var filter = new QQuery(string.Empty, UserReg.DBTable);
            filter.BuildPropertyParam(nameof(UserReg.UserId), CompareType.Equal, User.PrimaryId);
            filter.BuildPropertyParam(nameof(UserReg.RegType), CompareType.Equal, UserRegType.Password);
            filter.Orders.Add(new QOrder { Column = UserReg.DBTable.ParseProperty(nameof(UserReg.Id)), Direction = ListSortDirection.Descending });
            return UserReg.DBTable.Load(filter, DBLoadParam.Load | DBLoadParam.Synchronize);
        }

        public static async Task<User> GetUser(string login, string passoword)
        {
            var query = new QQuery(string.Empty, User.DBTable);
            query.BuildPropertyParam(nameof(Login), CompareType.Equal, login);
            query.BuildPropertyParam(nameof(Password), CompareType.Equal, passoword);
            var user = User.DBTable.Select(query).FirstOrDefault();
            if (user != null)
            {
                await UserReg.LogUser(user, UserRegType.Authorization, "GetUser");
            }

            return user;
        }

        [ControllerMethod(Anonymous = true)]
        public static async Task<TokenModel> LoginIn(LoginModel login)
        {
            var user = await StartSession(login);
            user.AccessToken = CreateAccessToken(user);
            user.RefreshToken = login.Online ? CreateRefreshToken(user) : null;
            await user.Save(user);
            return new TokenModel
            {
                Email = user.EMail,
                AccessToken = user.AccessToken,
                RefreshToken = user.RefreshToken
            };
        }

        [ControllerMethod(Anonymous = true)]
        public static async Task<TokenModel> ReLogin(TokenModel token)
        {
            var user = await StartSession(token.Email);

            if (user.RefreshToken == null || token.RefreshToken == null)
            {
                throw new Exception("Refresh token not found.");
            }
            else if (!user.RefreshToken.Equals(token.RefreshToken, StringComparison.Ordinal))
            {
                throw new Exception("Refresh token is invalid.");
            }

            token.AccessToken =
                user.AccessToken = CreateAccessToken(user);
            await user.Save(user);
            return token;
        }

        [ControllerMethod]
        public static async Task<TokenModel> LoginOut(TokenModel token, DBTransaction transaction)
        {
            var user = GetByEmail(token.Email) ?? GetByLogin(token.Email);
            if (user != transaction.Caller)
            {
                throw new Exception("Invalid Arguments!");
            }
            token.AccessToken =
                token.RefreshToken =
            user.AccessToken =
                user.RefreshToken = null;
            await user.Save(transaction);
            return token;
        }

        [ControllerMethod]
        public static async Task<bool> ChangePassword(LoginModel login, DBTransaction transaction)
        {
            var user = GetByEmail(login.Email) ?? GetByLogin(login.Email);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with Login\\Email {login.Email} not Found!");
            }

            if (user != transaction.Caller)
            {
                if (!user.Access.GetFlag(AccessType.Admin, transaction.Caller)
                    && !user.Table.Access.GetFlag(AccessType.Admin, transaction.Caller))
                {
                    throw new UnauthorizedAccessException();
                }
            }
            user.Password = Helper.Decript(login.Password, SMTPSetting.Current.PassKey);
            await user.Save(transaction);
            return true;
        }

        private static string CreateRefreshToken(User user)
        {
            return Helper.GetSha256(user.EMail + Guid.NewGuid().ToString());
        }

        private static string CreateAccessToken(User user)
        {
            var identity = GetIdentity(user);
            var now = DateTime.UtcNow;

            var jwt = new JwtSecurityToken(
                    issuer: JwtSetting.Current.ValidIssuer,
                    audience: JwtSetting.Current.ValidAudience,
                    notBefore: now,
                    expires: now.AddMinutes(JwtSetting.Current.LifeTime),
                    claims: identity.Claims,
                    signingCredentials: JwtSetting.Current.SigningCredentials);
            var jwthandler = new JwtSecurityTokenHandler();
            return jwthandler.WriteToken(jwt);
        }

        private static ClaimsIdentity GetIdentity(User user)
        {
            var claimsIdentity = new ClaimsIdentity(
                identity: user,
                claims: GetClaims(user),
                authenticationType: AuthenticationScheme,
                nameType: JwtRegisteredClaimNames.NameId,
                roleType: "");
            return claimsIdentity;
        }

        private static IEnumerable<Claim> GetClaims(User person)
        {
            if (person.ExternalId != null)
            {
                yield return new Claim("sub", person.ExternalId.ToString());
            }
            yield return new Claim(ClaimTypes.Name, person.EMail);
            yield return new Claim(ClaimTypes.Email, person.EMail);
        }

        protected bool online = false;
        private Company company;
        private Department department;
        private Position position;
        private List<IAccessIdentity> identities;
        private AccessValue cacheAccess;
        private Address address;

        public User()
        { }

        public UserReg LogStart { get; set; }

        public override int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(ExternalIdKey);
            set => SetValue(value, ExternalIdKey);
        }

        [Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get => GetValue<int?>(CompanyKey);
            set => SetValue(value, CompanyKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(CompanyKey, ref company);
            set => SetReference(company = value, CompanyKey);
        }
        public override string Login
        {
            get => GetValue<string>(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("abbreviation", 4, Keys = DBColumnKeys.Indexing), Index("ruser_abbreviation", true)]
        public string Abbreviation
        {
            get => GetValue<string>(AbbreviationKey);
            set => SetValue(value, AbbreviationKey);
        }

        [Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public override string Name
        {
            get => GetName();
            set => SetName(value);
        }

        [Column("department_id"), Browsable(false)]
        public int? DepartmentId
        {
            get => GetValue<int?>(DepartmentKey);
            set => SetValue(value, DepartmentKey);
        }

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get => GetReference(DepartmentKey, ref department);
            set => SetReference(department = value, DepartmentKey);
        }

        [Column("position_id"), Browsable(false)]
        public int? PositionId
        {
            get => GetValue<int?>(PositionKey);
            set => SetValue(value, PositionKey);
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get => GetReference<Position>(PositionKey, ref position);
            set
            {
                SetReference(position = value, PositionKey);
                Department = value?.Department;
            }
        }

        [ReadOnly(true)]
        [DefaultValue(false), Column("super")]
        public bool? Super
        {
            get => GetValue<bool?>(SuperKey);
            set => SetValue(value, SuperKey);
        }

        [Browsable(false)]
        public bool Online
        {
            get => online;
            set
            {
                if (online == value)
                    return;
                online = value;
                OnPropertyChanged();
            }
        }

        public override string EMail
        {
            get => GetValue<string>(EmailKey);
            set => SetValue(value, EmailKey);
        }

        [Column("phone", 1024), Index("ruser_phone", false)]
        public string Phone
        {
            get => GetValue<string>(PhoneKey);
            set => SetValue(value, PhoneKey);
        }

        public bool IsBlock
        {
            get => Status != DBStatus.Actual;
            set => Status = value ? DBStatus.Actual : DBStatus.Error;
        }

        [Column("is_temporary_password")]
        public bool? IsTemporaryPassword
        {
            get => GetValue<bool?>(IsTemporaryPasswordKey);
            set => SetValue(value, IsTemporaryPasswordKey);
        }

        [Column("password", 512, Keys = DBColumnKeys.Password), PasswordPropertyText(true)]
        public string Password
        {
            get => GetValue<string>(PasswordKey);
            set
            {
                if (value == null)
                {
                    SetValue(value, PasswordKey);
                    return;
                }
                var rez = ValidateText(this, value);
                if (!string.IsNullOrEmpty(rez))
                {
                    throw new ArgumentException(rez);
                }
                IsTemporaryPassword = false;
                SetValue(Helper.GetSha512(value), PasswordKey);
            }
        }

        [Browsable(false)]
        public override AccessValue Access
        {
            get => base.Access != Table.Access ? base.Access
                  : Department?.Access ?? Position?.Access ?? Table.Access;
        }

        [Browsable(false)]
        public string AccessToken { get; set; }

        [Browsable(false), Column("token_refresh", 2048, Keys = DBColumnKeys.Password | DBColumnKeys.NoLog)]
        public string RefreshToken
        {
            get => GetValue<string>(RefreshTokenKey);
            set => SetValue(value, RefreshTokenKey);
        }

        [Column("auth_type")]
        public UserAuthType? AuthType
        {
            get => GetValue<UserAuthType?>(AuthTokenKey) ?? UserAuthType.SMTP;
            set => SetValue(value, AuthTokenKey);
        }

        public override bool IsAuthenticated => string.IsNullOrEmpty(AccessToken);

        public string NameRU
        {
            get => GetValue<string>(NameRUKey);
            set => SetValue(value, NameRUKey);
        }

        public string NameEN
        {
            get => GetValue<string>(NameENKey);
            set => SetValue(value, NameENKey);
        }

        [Column("address_id"), Browsable(false)]
        public int? AddessId
        {
            get => GetValue<int?>(AddressIdKey);
            set => SetValue(value, AddressIdKey);
        }

        [Reference(nameof(AddessId))]
        public Address Address
        {
            get => GetReference(AddressIdKey, ref address);
            set => SetReference(address = value, AddressIdKey);
        }
        public override string AuthenticationType => AuthType?.ToString();

        public override IEnumerable<IAccessIdentity> Groups
        {
            get
            {
                var access = Access;
                if (identities == null)
                {
                    cacheAccess = access;
                    identities = new List<IAccessIdentity>(Super ?? false
                            ? (IEnumerable<IAccessIdentity>)UserGroup.DBTable
                            : access.Items.Where(p => p.Create).Select(p => p.Identity));
                    identities.Add(this);
                }
                else if (access != cacheAccess)
                {
                    cacheAccess = access;
                    identities.Clear();
                    identities.Add(this);
                    identities.AddRange(Super ?? false
                        ? (IEnumerable<IAccessIdentity>)UserGroup.DBTable
                        : access.Items.Where(p => p.Create).Select(p => p.Identity));
                }
                return identities;
            }
        }


        public override void Dispose()
        {
            base.Dispose();
        }

        public new int CompareTo(object obj)
        {
            return base.CompareTo(obj);// this.ToString().CompareTo(obj.ToString());
        }
    }

    public enum UserAuthType
    {
        Internal = 1,
        SMTP = 2,
        LDAP = 3
    }
}
