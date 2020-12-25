﻿using DataWF.Common;
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
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{
    public partial class UserTable : DBTable<User>
    {
        private static PasswordSpec PasswordSpecification = PasswordSpec.Lenght6 | PasswordSpec.CharSpecial | PasswordSpec.CharNumbers;
        public const string AuthenticationScheme = "Bearer";

        public User GetByEmail(string email)
        {
            return SelectOne(EmailKey, email);
        }

        public User GetByLogin(string login)
        {
            return SelectOne(CodeKey, login);
        }

        public User GetByEnvironment()
        {
            return LoadByCode(Environment.UserName);
        }

        public async Task RegisterSession(User user, LoginModel login = null)
        {
            if (user == null || user.LogStart != null)
            {
                return;
            }
            var text = login == null ? $"Login:{user.EMail}" : $"Login:{user.EMail}\nPlatform:{login.Platform}\nApp:{login.Application}\nVersion:{login.Version}";
            await UserReg.LogUser(user, UserRegType.Authorization, text);
        }

        public Task<User> StartSession(string login, string password)
        {
            return StartSession(new LoginModel { Email = login, Password = password, Platform = "unknown", Application = "unknown", Version = "1.0.0.0" });
        }

        public async Task<User> StartSession(string email)
        {
            var user = GetByEmail(email) ?? GetByLogin(email);
            if (user == null || user.Status == DBStatus.Archive || user.Status == DBStatus.Error)
            {
                throw new KeyNotFoundException("User not found!");
            }

            await RegisterSession(user);
            return user;
        }

        public async Task<User> StartSession(LoginModel login)
        {
            var user = GetByEmail(login.Email) ?? GetByLogin(login.Email);
            if (user == null || user.Status == DBStatus.Archive || user.Status == DBStatus.Error)
            {
                throw new KeyNotFoundException("User not found!");
            }
            var password = SMTPSetting.Current?.PassKey == null ? login.Password : Helper.Decript(login.Password, SMTPSetting.Current.PassKey);

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

        public string ValidateText(User user, string password)
        {
            string message = Helper.PasswordVerification(password, user.Login, PasswordSpecification);
            var proc = Schema?.Procedures["simple"];
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

        public IEnumerable<UserReg> GetOld(User User)
        {
            var regTable = (UserRegTable)Schema.GetTable<UserReg>();
            var filter = new QQuery(string.Empty, regTable);
            filter.BuildParam(regTable.UserKey, CompareType.Equal, User.PrimaryId);
            filter.BuildParam(regTable.RegTypeKey, CompareType.Equal, UserRegType.Password);
            filter.Orders.Add(new QOrder(regTable.PrimaryKey) { Direction = ListSortDirection.Descending });
            return regTable.Load(filter, DBLoadParam.Load | DBLoadParam.Synchronize);
        }

        public async Task<User> GetUser(string login, string passoword)
        {
            var query = new QQuery(string.Empty, this);
            query.BuildParam(LoginKey, CompareType.Equal, login);
            query.BuildParam(PasswordKey, CompareType.Equal, passoword);
            var user = Select(query).FirstOrDefault();
            if (user != null)
            {
                await UserReg.LogUser(user, UserRegType.Authorization, "GetUser");
            }

            return user;
        }

        [ControllerMethod(Anonymous = true)]
        public async Task<TokenModel> LoginIn(LoginModel login)
        {
            var user = await StartSession(login);
            user.AccessToken = CreateAccessToken(user);
            user.RefreshToken = (login.Online ?? false) ? CreateRefreshToken(user) : null;
            await user.Save(user);
            return new TokenModel
            {
                Email = user.EMail,
                AccessToken = user.AccessToken,
                RefreshToken = user.RefreshToken
            };
        }

        [ControllerMethod(Anonymous = true)]
        public async Task<TokenModel> ReLogin(TokenModel token)
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
        public async Task<TokenModel> LoginOut(TokenModel token, DBTransaction transaction)
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
        public async Task<bool> ChangePassword(LoginModel login, DBTransaction transaction)
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


    }

    [Table("ruser", "User", BlockSize = 100, Type = typeof(UserTable)), InvokerGenerator]
    public partial class User : DBUser, IComparable, IDisposable
    {
        protected bool online = false;
        private Company company;
        private Department department;
        private Position position;
        private List<IAccessIdentity> identities;
        private AccessValue cacheAccess;
        private Address address;

        public User()
        { }

        [JsonIgnore]
        public UserReg LogStart { get; set; }

        [JsonIgnore]
        public UserTable UserTable => (UserTable)Table;

        public override int Id
        {
            get => GetValue<int>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(UserTable.ExternalKey);
            set => SetValue(value, UserTable.ExternalKey);
        }

        [Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get => GetValue<int?>(UserTable.CompanyKey);
            set => SetValue(value, UserTable.CompanyKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(UserTable.CompanyKey, ref company);
            set => SetReference(company = value, UserTable.CompanyKey);
        }
        public override string Login
        {
            get => GetValue<string>(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("abbreviation", 4, Keys = DBColumnKeys.Indexing), Index("ruser_abbreviation", true)]
        public string Abbreviation
        {
            get => GetValue<string>(UserTable.AbbreviationKey);
            set => SetValue(value, UserTable.AbbreviationKey);
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
            get => GetValue<int?>(UserTable.DepartmentKey);
            set => SetValue(value, UserTable.DepartmentKey);
        }

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get => GetReference(UserTable.DepartmentKey, ref department);
            set => SetReference(department = value, UserTable.DepartmentKey);
        }

        [Column("position_id"), Browsable(false)]
        public int? PositionId
        {
            get => GetValue<int?>(UserTable.PositionKey);
            set => SetValue(value, UserTable.PositionKey);
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get => GetReference<Position>(UserTable.PositionKey, ref position);
            set
            {
                SetReference(position = value, UserTable.PositionKey);
                Department = value?.Department;
            }
        }

        [ReadOnly(true)]
        [DefaultValue(false), Column("super")]
        public bool? Super
        {
            get => GetValue<bool?>(UserTable.SuperKey);
            set => SetValue(value, UserTable.SuperKey);
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
                OnPropertyChanged<bool>();
            }
        }

        public override string EMail
        {
            get => GetValue<string>(UserTable.EmailKey);
            set => SetValue(value, UserTable.EmailKey);
        }

        [Column("phone", 1024), Index("ruser_phone", false)]
        public string Phone
        {
            get => GetValue<string>(UserTable.PhoneKey);
            set => SetValue(value, UserTable.PhoneKey);
        }

        public bool IsBlock
        {
            get => Status != DBStatus.Actual;
            set => Status = value ? DBStatus.Actual : DBStatus.Error;
        }

        [Column("is_temp_pass")]
        public bool? IsTempPassword
        {
            get => GetValue<bool?>(UserTable.IsTemPassKey);
            set => SetValue(value, UserTable.IsTemPassKey);
        }

        [Column("password", 512, Keys = DBColumnKeys.Password), PasswordPropertyText(true)]
        public string Password
        {
            get => GetValue<string>(UserTable.PasswordKey);
            set
            {
                if (value == null)
                {
                    SetValue(value, UserTable.PasswordKey);
                    return;
                }
                var rez = UserTable.ValidateText(this, value);
                if (!string.IsNullOrEmpty(rez))
                {
                    throw new ArgumentException(rez);
                }
                IsTempPassword = false;
                SetValue(Helper.GetSha512(value), UserTable.PasswordKey);
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
            get => GetValue<string>(UserTable.RefreshTokenKey);
            set => SetValue(value, UserTable.RefreshTokenKey);
        }

        [Column("auth_type")]
        public UserAuthType? AuthType
        {
            get => GetValue<UserAuthType?>(UserTable.AuthTokenKey) ?? UserAuthType.SMTP;
            set => SetValue(value, UserTable.AuthTokenKey);
        }

        public override bool IsAuthenticated => string.IsNullOrEmpty(AccessToken);

        public string NameRU
        {
            get => GetValue<string>(UserTable.NameRUKey);
            set => SetValue(value, UserTable.NameRUKey);
        }

        public string NameEN
        {
            get => GetValue<string>(UserTable.NameENKey);
            set => SetValue(value, UserTable.NameENKey);
        }

        [Column("address_id"), Browsable(false)]
        public int? AddessId
        {
            get => GetValue<int?>(UserTable.AddressKey);
            set => SetValue(value, UserTable.AddressKey);
        }

        [Reference(nameof(AddessId))]
        public Address Address
        {
            get => GetReference(UserTable.AddressKey, ref address);
            set => SetReference(address = value, UserTable.AddressKey);
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
