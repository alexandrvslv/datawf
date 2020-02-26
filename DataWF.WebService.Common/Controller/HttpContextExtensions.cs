using Microsoft.AspNetCore.Http;

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
    }
}
