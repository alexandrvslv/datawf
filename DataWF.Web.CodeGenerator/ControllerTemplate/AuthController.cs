using DataWF.Module.Common;
using DataWF.Web.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;

namespace DataWF.Web.Controller
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IOptions<JwtAuth> jwtAuth;

        public AuthController(IOptions<JwtAuth> jwtAuth)
        {
            this.jwtAuth = jwtAuth ?? throw new ArgumentNullException(nameof(jwtAuth));
        }

        [AllowAnonymous]
        [HttpPost()]
        public ActionResult<string> Login([FromBody]LoginModel login)
        {
            var user = (User)null;
            var identity = (ClaimsIdentity)null;
            try
            {
                identity = GetIdentity(login, out user);
            }
            catch
            {
                return BadRequest("Invalid email or password.");
            }

            var now = DateTime.UtcNow;

            var jwt = new JwtSecurityToken(
                    issuer: jwtAuth.Value.ValidIssuer,
                    audience: jwtAuth.Value.ValidAudience,
                    notBefore: now,
                    expires: now.AddMinutes(jwtAuth.Value.LifeTime),
                    claims: identity.Claims,
                    signingCredentials: jwtAuth.Value.SigningCredentials);
            user.Token = new JwtSecurityTokenHandler().WriteToken(jwt);
            return Ok(user.Token);
        }

        private ClaimsIdentity GetIdentity(LoginModel login, out User user)
        {
            var credentials = new NetworkCredential(login.Email, login.Password);
            user = DataWF.Module.Common.User.SetCurrentByEmail(credentials, true);
            if (user != null)
            {
                ClaimsIdentity claimsIdentity = new ClaimsIdentity(
                    identity: user,
                    claims: GetClaims(user),
                    authenticationType: JwtBearerDefaults.AuthenticationScheme,
                    nameType: JwtRegisteredClaimNames.NameId,
                    roleType: "");
                return claimsIdentity;
            }

            // если пользователя не найдено
            return null;
        }

        private IEnumerable<Claim> GetClaims(User person)
        {
            yield return new Claim(JwtRegisteredClaimNames.NameId, person.EMail);
            yield return new Claim(JwtRegisteredClaimNames.Email, person.EMail);
        }
    }
}
