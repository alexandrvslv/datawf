using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace DataWF.WebService.Common
{
    public static class HttpContextExtensions
    {
        public static bool? ReadBool(this HttpContext httpContext, string key)
        {
            var header = httpContext?.Request.Headers;
            if (header != null
                && header.TryGetValue(key, out var values)
                && values.Count > 0
                && bool.TryParse(values[0], out var boolValue))
            {
                return boolValue;
            }
            return null;
        }

        public static int? ReadInt(this HttpContext httpContext, string key)
        {
            var header = httpContext?.Request.Headers;
            if (header != null
                && header.TryGetValue(key, out var values)
                && values.Count > 0
                && int.TryParse(values[0], out var intValue))
            {
                return intValue;
            }
            return null;
        }

        public static void DisableBuffering(this HttpContext httpContext)
        {
            var responseBodyFeature = httpContext.Features.Get<IHttpResponseBodyFeature>();
            if (responseBodyFeature != null)
            {
                responseBodyFeature.DisableBuffering();
            }
        }
    }
}
