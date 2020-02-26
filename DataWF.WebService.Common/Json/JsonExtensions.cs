using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.WebService.Common
{
    public static class JsonExtensions
    {
        public static void InitDefaults(this JsonSerializerOptions options, HttpContext httpContext = null)
        {
            options.InitDefaults(new DBItemJsonConverter { HttpContext = httpContext });
        }

        public static void InitDefaults(this JsonSerializerOptions options, DBItemJsonConverter converter)
        {
#if DEBUG
            options.WriteIndented = true;
#endif
            // Use the default property (As Is).
            options.PropertyNamingPolicy = null;
            options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            // Configure a converters.
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(converter);
        }
    }
}
