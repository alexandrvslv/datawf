using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Text.Json;

namespace DataWF.WebService.Common
{
    public class LoggerAndFormatterAttribute : ActionFilterAttribute
    {
        public LoggerAndFormatterAttribute()
        { }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            WebNotifyService.Instance.SetCurrentAction(context);
        }

        //https://stackoverflow.com/a/52623772/4682355
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            //options.OutputFormatters.RemoveType<JsonOutputFormatter>();
            //var settings = new JsonSerializerSettings() { ContractResolver = DBItemContractResolver.Instance };
            //settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            //options.OutputFormatters.Insert(0, new ClaimsJsonOutputFormatter(settings, ArrayPool<char>.Shared));
            //memory leak
            //if (context.Result is ObjectResult objectResult)
            //{
            //    var options = new JsonSerializerOptions();
            //    options.InitDefaults(context.HttpContext);
            //    objectResult.Formatters.Add(new SystemTextJsonOutputFormatter(options));
            //}
        }
    }
}
