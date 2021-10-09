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
            return Provider.GetTable(typeToConvert) != null;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter)Provider.GetTable(typeToConvert)?.Converter;
        }
    }
}