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
            var user = context.HttpContext.User?.Identity as User;
            System.Diagnostics.Debug.WriteLine($"{context.ActionDescriptor.DisplayName}, {user}");
            //User.SetCurrentByEmail(email, true);
        }
    }
}
