using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.Common
{
    public class JsonClientConverterFactory : JsonConverterFactory
    {
        public JsonClientConverterFactory(IClientProvider provider)
        {
            Provider = provider;
        }

        public IClientProvider Provider { get; }
        public override bool CanConvert(Type typeToConvert)
        {
            return Provider.GetClient(typeToConvert) != null;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter)Provider.GetClient(typeToConvert)?.Converter;
        }
    }
}