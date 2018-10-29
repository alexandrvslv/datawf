using DataWF.Module.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;

namespace DataWF.Web.Common
{
    public class AuthAttribute : TypeFilterAttribute
    {
        public AuthAttribute() : base(typeof(CurentUserFilter))
        { }
    }

    public class CurentUserFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var emailClaim = context.HttpContext.User?.FindFirst(ClaimTypes.Email);
            var user = emailClaim != null ? User.GetByEmail(emailClaim.Value) : null;
            Debug.WriteLine($"{context.ActionDescriptor.DisplayName}({context.ActionDescriptor.Parameters.FirstOrDefault()?.Name}) {user}");
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var factories = context.ValueProviderFactories;
            factories.RemoveType<FormValueProviderFactory>();
            factories.RemoveType<JQueryFormValueProviderFactory>();
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }
    }
}
