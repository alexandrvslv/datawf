using DataWF.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.WebService.Common
{
    public static class JsonExtensions
    {
        public static void InitDefaults(this JsonSerializerOptions options, HttpContext httpContext = null)
        {
            options.InitDefaults(new DBItemConverterFactory(httpContext));
        }

        public static void InitDefaults(this JsonSerializerOptions options, DBItemConverterFactory converter)
        {
#if DEBUG
            options.WriteIndented = true;
#endif
            options.IgnoreNullValues = false;
            options.IgnoreReadOnlyProperties = false;
            options.DefaultBufferSize = 82 * 1024;
            options.PropertyNamingPolicy = null;
            options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            // Configure a converters.
            //options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new TimeSpanConverter());
            options.Converters.Add(converter);
        }
    }
}
