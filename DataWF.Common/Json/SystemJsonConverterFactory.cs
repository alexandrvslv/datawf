using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.Common
{
    public class SystemJsonConverterFactory : JsonConverterFactory
    {
        public static ConcurrentDictionary<Utf8JsonWriter, ClientSerializationContext> WriterContexts = new ConcurrentDictionary<Utf8JsonWriter, ClientSerializationContext>();

        public SystemJsonConverterFactory(IWebSchema provider)
        {
            Provider = provider;
        }

        public IWebSchema Provider { get; }

        public override bool CanConvert(Type typeToConvert)
        {
            return Provider.GetTable(typeToConvert) != null
                || (typeToConvert.IsClass && !TypeHelper.IsEnumerable(typeToConvert)
                    && typeToConvert != typeof(string)
                    && typeToConvert != typeof(byte[]));
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var table = Provider.GetTable(typeToConvert);
            if (table != null)
            {
                return (JsonConverter)table.Converter;
            }
            else if (typeToConvert.IsClass)
            {
                return (JsonConverter)TypeHelper.GetClassSerializer(typeToConvert);
            }
            else
            {
                return options.GetConverter(typeToConvert);
            }
        }
    }
}