using DataWF.Common;
using DataWF.WebClient.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;

namespace DataWF.WebService.Common
{
    public static class HttpContextExtensions
    {
        public static HttpJsonSettings ReadJsonSettings(this HttpContext httpContext)
        {
            var keys = httpContext.ReadEnum<HttpJsonKeys>(HttpJsonSettings.XJsonKeys);

            var fullJson = httpContext.Request.Headers[HttpJsonSettings.XGetFullJson];
            if (keys == null)
            {
                keys = HttpJsonKeys.None;
                if (httpContext.ReadBool(HttpJsonSettings.XJsonKeyRefered) ?? true)
                {
                    keys |= HttpJsonKeys.Refed;
                }
                if (httpContext.ReadBool(HttpJsonSettings.XJsonKyeRefering) ?? true)
                {
                    keys |= HttpJsonKeys.Refing;
                }
                if (httpContext.ReadBool(HttpJsonSettings.XJsonKeyRef) ?? true)
                {
                    keys |= HttpJsonKeys.Ref;
                }
                if (fullJson.ToString() != null)
                {

                }
            }
            var maxDepth = httpContext.ReadInt(HttpJsonSettings.XJsonMaxDepth) ?? 4;
            bool.TryParse(fullJson, out var result);
            if (result)
            {
                return new HttpJsonSettings((HttpJsonKeys)keys, maxDepth, result);

            }
            else
            {
                return new HttpJsonSettings((HttpJsonKeys)keys, maxDepth);
            }
        }

        public static HttpPageSettings ReadPageSettings(this HttpContext httpContext)
        {
            var listFrom = httpContext.ReadInt(HttpPageSettings.XListFrom);
            var listTo = httpContext.ReadInt(HttpPageSettings.XListTo);
            if (listTo != null)
            {
                return HttpPageSettings.FromList(listFrom ?? 0, listTo.Value);
            }

            var pageIndex = httpContext.ReadInt(HttpPageSettings.XPageIndex);
            var pageSize = httpContext.ReadInt(HttpPageSettings.XPageSize);
            if (pageIndex != null)
            {
                return HttpPageSettings.FromPage(pageIndex.Value, pageSize ?? 50);
            }

            return null;
        }

        public static bool WritePageSettings(this HttpContext httpContext, HttpPageSettings pageSettings)
        {
            return httpContext.WriteInt(HttpPageSettings.XListCount, pageSettings.ListCount)
                && httpContext.WriteInt(HttpPageSettings.XListFrom, pageSettings.ListFrom)
                && httpContext.WriteInt(HttpPageSettings.XListTo, pageSettings.ListTo)
                && httpContext.WriteInt(HttpPageSettings.XPageCount, pageSettings.PageCount)
                && httpContext.WriteInt(HttpPageSettings.XPageIndex, pageSettings.PageIndex)
                && httpContext.WriteInt(HttpPageSettings.XPageSize, pageSettings.PageSize);
        }

        public static T? ReadEnum<T>(this HttpContext httpContext, string key) where T : struct
        {
            return httpContext.Request?.ReadEnum<T>(key);
        }

        public static T? ReadEnum<T>(this HttpRequest request, string key) where T : struct
        {
            return request.Headers?.ReadEnum<T>(key);
        }

        public static T? ReadEnum<T>(this IHeaderDictionary header, string key) where T : struct
        {
            if (header.TryGetValue(key, out var values)
                && values.Count > 0
                && Enum.TryParse<T>(values[0], out var enumValue))
            {
                return enumValue;
            }
            return null;
        }

        public static bool? ReadBool(this HttpContext httpContext, string key)
        {
            return httpContext.Request?.ReadBool(key);
        }

        public static bool? ReadBool(this HttpRequest request, string key)
        {
            return request.Headers?.ReadBool(key);
        }

        public static bool? ReadBool(this IHeaderDictionary header, string key)
        {
            if (header.TryGetValue(key, out var values)
                && values.Count > 0
                && bool.TryParse(values[0], out var boolValue))
            {
                return boolValue;
            }
            return null;
        }

        public static int? ReadInt(this HttpContext httpContext, string key)
        {
            return httpContext.Request?.ReadInt(key);
        }

        public static int? ReadInt(this HttpRequest request, string key)
        {
            return request.Headers?.ReadInt(key);
        }

        public static int? ReadInt(this IHeaderDictionary header, string key)
        {
            if (header.TryGetValue(key, out var values)
                && values.Count > 0
                && int.TryParse(values[0], out var intValue))
            {
                return intValue;
            }
            return null;
        }

        public static bool WriteInt(this HttpContext httpContext, string key, int value)
        {
            return httpContext.Response?.WriteInt(key, value) ?? false;
        }

        public static bool WriteInt(this HttpResponse responce, string key, int value)
        {
            return responce.Headers?.WriteInt(key, value) ?? false;
        }

        public static bool WriteInt(this IHeaderDictionary header, string key, int value)
        {
            return header.TryAdd(key, new StringValues(value.ToString()));
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
