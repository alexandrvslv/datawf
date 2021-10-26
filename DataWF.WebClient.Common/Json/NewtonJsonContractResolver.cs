using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DataWF.Common
{
    public class NewtonJsonContractResolver : DefaultContractResolver
    {
        public static ConcurrentDictionary<JsonWriter, ClientSerializationContext> WriterContexts = new ConcurrentDictionary<JsonWriter, ClientSerializationContext>();
        public NewtonJsonContractResolver(IClientProvider provider)
        {
            Provider = provider;
        }

        public IClientProvider Provider { get; }

        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            return (JsonConverter)Provider.GetClient(objectType)?.Converter ??
            base.ResolveContractConverter(objectType);
        }
    }
}