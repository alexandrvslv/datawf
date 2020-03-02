using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataWF.WebService.Common
{
    //https://github.com/dotnet/aspnetcore/blob/master/src/Mvc/Mvc.Core/src/Formatters/MediaTypeHeaderValues.cs
    public class DBItemOutputFormatter : TextOutputFormatter
    {
        public static readonly MediaTypeHeaderValue ApplicationJson
            = MediaTypeHeaderValue.Parse("application/json").CopyAsReadOnly();

        public static readonly MediaTypeHeaderValue TextJson
            = MediaTypeHeaderValue.Parse("text/json").CopyAsReadOnly();

        public static readonly MediaTypeHeaderValue ApplicationAnyJsonSyntax
            = MediaTypeHeaderValue.Parse("application/*+json").CopyAsReadOnly();

        public DBItemOutputFormatter()
        {
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
            SupportedMediaTypes.Add(ApplicationJson);
            SupportedMediaTypes.Add(TextJson);
            SupportedMediaTypes.Add(ApplicationAnyJsonSyntax);
        }

        /// <inheritdoc />
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
            var option = new JsonSerializerOptions();
            option.InitDefaults(httpContext);
            var writeStream = httpContext.Response.Body;
            var objectType = context.Object?.GetType() ?? context.ObjectType ?? typeof(object);
            await JsonSerializer.SerializeAsync(writeStream, context.Object, objectType, option);
            await writeStream.FlushAsync();
        }

    }
}