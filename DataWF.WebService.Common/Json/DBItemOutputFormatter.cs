using DataWF.Common;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections;
using System.IO;
using System.IO.Pipelines;
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
        private static readonly byte[] commaArray = Encoding.UTF8.GetBytes(",");
        private static readonly byte start = startArray[0];
        private static readonly byte end = endArray[0];
        private static readonly byte comma = commaArray[0];

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
            using (var factory = new DBItemConverterFactory(httpContext))
            {
                option.InitDefaults(factory);

                var objectType = context.Object?.GetType() ?? context.ObjectType ?? typeof(object);

                if (TypeHelper.IsEnumerable(objectType))
                {
                    var pipeWriter = httpContext.Response.BodyWriter;
                    await WriteArray(pipeWriter, (IEnumerable)context.Object, objectType, option);
                    await pipeWriter.FlushAsync();
                }
                else
                {
                    var pipeWriter = httpContext.Response.BodyWriter;
                    using (var jsonWriter = CreatetWriter(pipeWriter, option))
                    {
                        JsonSerializer.Serialize(jsonWriter, context.Object, objectType, option);
                    }
                    await pipeWriter.FlushAsync();
                }
            }
        }

        private async Task WriteArray(PipeWriter pipeWriter, IEnumerable enumerable, Type objectType, JsonSerializerOptions option)
        {
            var itemType = TypeHelper.GetItemType(objectType);
            {
                WriteByte(pipeWriter, start);
            }
            var bytes = 0L;
            var max = 64 * 1024;
            using (var jsonWriter = CreatetWriter(pipeWriter, option))
            {
                var commaSet = false;
                foreach (var item in enumerable)
                {
                    if (commaSet)
                    {
                        WriteByte(pipeWriter, comma);
                    }
                    try
                    {
                        JsonSerializer.Serialize(jsonWriter, item, itemType, option);
                        bytes += jsonWriter.BytesCommitted;
                        jsonWriter.Reset();
                        if (bytes > max)
                        {
                            await pipeWriter.FlushAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Helper.OnException(ex);
                    }
                    commaSet = true;
                }
            }
            WriteByte(pipeWriter, end);
        }

        private static void WriteByte(PipeWriter pipeWriter, byte value)
        {
            var span = pipeWriter.GetSpan();
            span[0] = value;
            pipeWriter.Advance(1);
        }

        private async Task WriteArray(Stream stream, IEnumerable enumerable, Type objectType, JsonSerializerOptions option)
        {
            var itemType = TypeHelper.GetItemType(objectType);
            await stream.WriteAsync(startArray);
            using (var memoryStream = new MemoryStream())
            using (var jsonWriter = CreatetWriter(memoryStream, option))
            {
                var commaSet = false;
                foreach (var item in enumerable)
                {
                    if (commaSet)
                    {
                        await stream.WriteAsync(commaArray);
                    }
                    try
                    {
                        JsonSerializer.Serialize(jsonWriter, item, itemType, option);

                        await stream.WriteAsync(memoryStream.GetBuffer(), 0, (int)jsonWriter.BytesCommitted);
                        await stream.FlushAsync();
                        jsonWriter.Reset();
                        memoryStream.Position = 0;
                    }
                    catch (Exception ex)
                    {
                        Helper.OnException(ex);
                    }
                    commaSet = true;
                }
            }
            await stream.WriteAsync(endArray);
        }

        private async Task WriteArray(Utf8JsonWriter jsonWriter, IEnumerable enumerable, Type objectType, JsonSerializerOptions option)
        {
            var itemType = TypeHelper.GetItemType(objectType);
            jsonWriter.WriteStartArray();
            foreach (var item in enumerable)
            {
                JsonSerializer.Serialize(jsonWriter, item, itemType, option);
                await jsonWriter.FlushAsync();
            }
            jsonWriter.WriteEndArray();
        }

        private static Utf8JsonWriter CreatetWriter(PipeWriter pipeWriter, JsonSerializerOptions option)
        {
            return new Utf8JsonWriter(pipeWriter,
                            new JsonWriterOptions
                            {
                                Indented = option.WriteIndented,
                                Encoder = option.Encoder,
                                SkipValidation = true
                            });
        }

        private static Utf8JsonWriter CreatetWriter(Stream stram, JsonSerializerOptions option)
        {
            return new Utf8JsonWriter(stram,
                            new JsonWriterOptions
                            {
                                Indented = option.WriteIndented,
                                Encoder = option.Encoder,
                                SkipValidation = true
                            });
        }
    }
}