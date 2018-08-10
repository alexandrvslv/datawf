using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Web.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DataWF.Web.Common
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    [Auth]
    public class AuthorizeController : ControllerBase
    {
        private readonly IOptions<JwtAuth> jwtAuth;
        private DBTable<User> users;
        public AuthorizeController(IOptions<JwtAuth> jwtAuth)
        {
            this.jwtAuth = jwtAuth ?? throw new ArgumentNullException(nameof(jwtAuth));
            users = DBTable.GetTable<User>();
        }

        [AllowAnonymous]
        [HttpPost("LoginIn/")]
        public ActionResult<TokenModel> LoginIn([FromBody]LoginModel login)
        {
            var user = (User)null;
            try
            {
                user = GetUser(login);
            }
            catch
            {
                return BadRequest("Invalid email or password.");
            }

            user.AccessToken = CreateAccessToken(user);
            if (login.Online)
            {
                user.RefreshToken = CreateRefreshToken(user);
            }
            else
            {
                user.RefreshToken = null;
            }

            user.Save();
            return new TokenModel { Email = user.EMail, AccessToken = user.AccessToken, RefreshToken = user.RefreshToken };
        }

        [AllowAnonymous]
        [HttpPost("ReLogin/")]
        public ActionResult<TokenModel> ReLogin([FromBody]TokenModel token)
        {
            var user = GetUser(token);
            if (user.RefreshToken == null || token.RefreshToken == null)
            {
                return BadRequest("Refresh token was not found.");
            }
            else if (user.RefreshToken != token.RefreshToken)
            {
                return BadRequest("Refresh token is invalid.");
            }
            token.AccessToken =
                user.AccessToken = CreateAccessToken(user);
            user.Save();
            return token;
        }

        [HttpPost("LoginOut/")]
        public ActionResult<TokenModel> LoginOut([FromBody]TokenModel token)
        {
            var user = GetUser(token);
            if (user != DataWF.Module.Common.User.CurrentUser)
            {
                return BadRequest("Invalid Arguments!");
            }
            token.AccessToken =
                token.RefreshToken =
            user.AccessToken =
                user.RefreshToken = null;
            user.Save();
            return token;
        }

        [HttpGet()]
        public ActionResult<User> Get()
        {
            return Ok(DataWF.Module.Common.User.CurrentUser);
        }

        private string CreateRefreshToken(User user)
        {
            return Helper.GetSha256(user.EMail + Guid.NewGuid().ToString());
        }

        private string CreateAccessToken(User user)
        {
            var identity = GetIdentity(user);
            var now = DateTime.UtcNow;

            var jwt = new JwtSecurityToken(
                    issuer: jwtAuth.Value.ValidIssuer,
                    audience: jwtAuth.Value.ValidAudience,
                    notBefore: now,
                    expires: now.AddMinutes(jwtAuth.Value.LifeTime),
                    claims: identity.Claims,
                    signingCredentials: jwtAuth.Value.SigningCredentials);
            var jwthandler = new JwtSecurityTokenHandler();
            return jwthandler.WriteToken(jwt);
        }

        private User GetUser(LoginModel login)
        {
            var credentials = new NetworkCredential(login.Email, login.Password);
            return DataWF.Module.Common.User.SetCurrentByEmail(credentials, true);
        }

        private User GetUser(TokenModel token)
        {
            return DataWF.Module.Common.User.SetCurrentByEmail(token.Email, true);
        }

        private ClaimsIdentity GetIdentity(User user)
        {
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(
                identity: user,
                claims: GetClaims(user),
                authenticationType: JwtBearerDefaults.AuthenticationScheme,
                nameType: JwtRegisteredClaimNames.NameId,
                roleType: "");
            return claimsIdentity;
        }

        private IEnumerable<Claim> GetClaims(User person)
        {
            yield return new Claim(ClaimTypes.Name, person.EMail);
            yield return new Claim(ClaimTypes.Email, person.EMail);
        }
    }
}
