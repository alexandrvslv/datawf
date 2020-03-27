using DataWF.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;

namespace DataWF.WebService.Common
{
    public static class HttpContextExtensions
    {
        public static HttpJsonSettings ReadJsonSettings(this HttpContext httpContext)
        {
            var keys = httpContext.ReadEnum<HttpJsonKeys>(HttpJsonSettings.JsonKeys);
            if (keys == null)
            {
                keys = HttpJsonKeys.None;
                if (httpContext.ReadBool(HttpJsonSettings.JsonReferenced) ?? true)
                {
                    keys |= HttpJsonKeys.Refed;
                }
                if (httpContext.ReadBool(HttpJsonSettings.JsonReferencing) ?? true)
                {
                    keys |= HttpJsonKeys.Refing;
                }
                if (httpContext.ReadBool(HttpJsonSettings.JsonReference) ?? true)
                {
                    keys |= HttpJsonKeys.Ref;
                }
            }
            var maxDepth = httpContext.ReadInt(HttpJsonSettings.JsonMaxDepth) ?? 4;
            return new HttpJsonSettings((HttpJsonKeys)keys, maxDepth);
        }

        public static T? ReadEnum<T>(this HttpContext httpContext, string key) where T : struct
        {
            var header = httpContext?.Request.Headers;
            if (header != null
                && header.TryGetValue(key, out var values)
                && values.Count > 0
                && Enum.TryParse<T>(values[0], out var enumValue))
            {
                return enumValue;
            }
            return null;
        }

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
