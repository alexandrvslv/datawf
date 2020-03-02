using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.WebService.Common
{
    public static class JsonExtensions
    {
        public static void InitAccessor(this JsonSerializerOptions options, IHttpContextAccessor httpContexAccessor)
        {
            options.InitDefaults(new DBItemJsonConverter { HttpContextAccessor = httpContexAccessor });
        }

        public static void InitDefaults(this JsonSerializerOptions options, HttpContext httpContext = null)
        {
            options.InitDefaults(new DBItemJsonConverter { HttpContext = httpContext });
        }

        public static void InitDefaults(this JsonSerializerOptions options, DBItemJsonConverter converter)
        {
#if DEBUG
            options.WriteIndented = true;
#endif
            options.IgnoreNullValues = false;
            options.DefaultBufferSize = 128 * 1024;
            options.PropertyNamingPolicy = null;
            options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            // Configure a converters.
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(converter);
        }
    }
}
