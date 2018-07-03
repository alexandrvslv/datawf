using DataWF.Module.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace DataWF.Web.Controller
{
    public class AuthAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var email = ((ControllerBase)context.Controller).User.Claims.FirstOrDefault(p => p.Type == JwtRegisteredClaimNames.Email)?.Value;
            System.Diagnostics.Debug.WriteLine(email);
            //User.SetCurrentByEmail(email, true);
        }
    }
}
