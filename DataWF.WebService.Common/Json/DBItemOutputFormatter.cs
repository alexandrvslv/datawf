using DataWF.Common;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataWF.WebService.Common
{
    //https://github.com/dotnet/aspnetcore/blob/master/src/Mvc/Mvc.Core/src/Formatters/SystemTextJsonOutputFormatter.cs
    public class DBItemOutputFormatter : TextOutputFormatter
    {
        private static readonly byte[] startArray = Encoding.UTF8.GetBytes("[");
        private static readonly byte[] endArray = Encoding.UTF8.GetBytes("]");
        private static readonly byte[] dot = Encoding.UTF8.GetBytes(",");
        public static readonly MediaTypeHeaderValue ApplicationJson
            = MediaTypeHeaderValue.Parse("application/json").CopyAsReadOnly();

        public static readonly MediaTypeHeaderValue TextJson
            = MediaTypeHeaderValue.Parse("text/json").CopyAsReadOnly();

        public static readonly MediaTypeHeaderValue ApplicationAnyJsonSyntax
            = MediaTypeHeaderValue.Parse("application/*+json").CopyAsReadOnly();

        public DBItemOutputFormatter()
        {
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedMediaTypes.Add(ApplicationJson);
            SupportedMediaTypes.Add(TextJson);
            SupportedMediaTypes.Add(ApplicationAnyJsonSyntax);
        }

        public sealed override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (selectedEncoding == null)
            {
                throw new ArgumentNullException(nameof(selectedEncoding));
            }

            var httpContext = context.HttpContext;

            httpContext.DisableBuffering();

            var option = new JsonSerializerOptions();
            option.InitDefaults(httpContext);

            var body = httpContext.Response.Body;
            var objectType = context.Object?.GetType() ?? context.ObjectType ?? typeof(object);

            //experiment
            //if (TypeHelper.IsEnumerable(objectType))
            //{
            //    await WriteArrayAsync(context, objectType, option);
            //}
            //else
            {
                await JsonSerializer.SerializeAsync(body, context.Object, objectType, option);
            }
            await body.FlushAsync();
        }

        private async Task WriteArrayAsync(OutputFormatterWriteContext context, Type objectType, JsonSerializerOptions option)
        {
            var itemType = TypeHelper.GetItemType(objectType);
            var body = context.HttpContext.Response.Body;
            await body.WriteAsync(startArray);
            var dotSet = false;
            foreach (var item in ((IEnumerable)context.Object))
            {
                if (dotSet)
                    await body.WriteAsync(dot);
                var buffer = JsonSerializer.SerializeToUtf8Bytes(item, itemType, option);
                await body.WriteAsync(buffer);
                dotSet = true;
            }
            await body.WriteAsync(endArray);
        }

        private void WriteArray(OutputFormatterWriteContext context, Type objectType, JsonSerializerOptions option)
        {
            var itemType = TypeHelper.GetItemType(objectType);
            var body = context.HttpContext.Response.Body;
            using (var jsonWriter = new Utf8JsonWriter(body, new JsonWriterOptions
            {
                Encoder = option.Encoder,
                Indented = option.WriteIndented,
                SkipValidation = true
            }))
            {
                jsonWriter.WriteStartArray();
                foreach (var item in ((IEnumerable)context.Object))
                {
                    JsonSerializer.Serialize(jsonWriter, item, itemType, option);
                }
                jsonWriter.WriteEndArray();
            }
        }
    }
}