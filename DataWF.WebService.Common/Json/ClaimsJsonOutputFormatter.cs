using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Formatters.Json.Internal;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.WebService.Common
{
    //https://github.com/aspnet/Mvc/blob/a67d9363e22be8ef63a1a62539991e1da3a6e30e/src/Microsoft.AspNetCore.Mvc.Formatters.Json/JsonOutputFormatter.cs
    public class ClaimsJsonOutputFormatter : JsonOutputFormatter
    {
        private readonly JsonArrayPool<char> pool;

        public ClaimsJsonOutputFormatter(JsonSerializerSettings serializerSettings, ArrayPool<char> charPool) : base(serializerSettings, charPool)
        {
            pool = new JsonArrayPool<char>(charPool);
        }

        protected override JsonSerializer CreateJsonSerializer()
        {
            return base.CreateJsonSerializer();
        }

        protected override JsonWriter CreateJsonWriter(TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var jsonWriter = new ClaimsJsonTextWriter(writer)
            {
                ArrayPool = pool,
                CloseOutput = false,
                AutoCompleteOnClose = false
            };

            return jsonWriter;
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (selectedEncoding == null)
            {
                throw new ArgumentNullException(nameof(selectedEncoding));
            }

            var response = context.HttpContext.Response;
            using (var writer = context.WriterFactory(response.Body, selectedEncoding))
            {
                WriteObject(writer, context, context.Object);

                // Perf: call FlushAsync to call WriteAsync on the stream with any content left in the TextWriter's
                // buffers. This is better than just letting dispose handle it (which would result in a synchronous
                // write).
                await writer.FlushAsync();
            }
        }

        private void WriteObject(TextWriter writer, OutputFormatterWriteContext context, object value)
        {
            using (var jsonWriter = (ClaimsJsonTextWriter)CreateJsonWriter(writer))
            {
                jsonWriter.UserPrincipal = context.HttpContext.User;
                var jsonSerializer = CreateJsonSerializer();
                jsonSerializer.Serialize(jsonWriter, value);
            }
        }
    }


}
