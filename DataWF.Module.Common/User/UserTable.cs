using DataWF.Common;
using DataWF.Data;
using MailKit.Net.Smtp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{
    public partial class UserTable : DBUserTable<User>
    {
        private static PasswordSpec PasswordSpecification = PasswordSpec.Lenght6 | PasswordSpec.CharSpecial | PasswordSpec.CharNumbers;
        public const string AuthenticationScheme = "Bearer";

        public User GetByEmail(string email)
        {
            return EMailKey.SelectOne<User>(email);
        }

        public User GetByLogin(string login)
        {
            return LoginKey.SelectOne<User>(login);
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
            var regTable = (UserRegTable)Schema.GetTable<UserReg>();
            await regTable.LogUser(user, UserRegType.Authorization, text);
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
            var proc = Schema.Procedures["simple"];
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
            var regTable = Schema.UserReg;
            var query = regTable.Query(DBLoadParam.Load | DBLoadParam.Synchronize)
                .Where(regTable.UserIdKey, CompareType.Equal, User.PrimaryId)
                .And(regTable.RegTypeKey, CompareType.Equal, UserRegType.Password)
                .OrderBy(regTable.PrimaryKey, ListSortDirection.Descending);
            return regTable.Load(query);
        }

        public async Task<User> GetUser(string login, string passoword)
        {
            var regTable = Schema.UserReg;
            var query = Query(DBLoadParam.Load)
                .Where(LoginKey, CompareType.Equal, login)
                .And(PasswordKey, CompareType.Equal, passoword);
            var user = Select(query).FirstOrDefault();
            if (user != null)
            {
                await regTable.LogUser(user, UserRegType.Authorization, "GetUser");
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
}
