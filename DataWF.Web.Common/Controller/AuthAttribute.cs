using DataWF.Module.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace DataWF.Web.Common
{
    public class AuthAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var emailClaim = context.HttpContext.User?.FindFirst(ClaimTypes.Email);
            if (emailClaim != null)
                User.SetCurrentByEmail(emailClaim.Value, true);
            System.Diagnostics.Debug.WriteLine($"{context.ActionDescriptor.DisplayName}, {User.CurrentUser}");
        }
    }
}
