using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace DataWF.Common
{
    public class NewtonJsonContractResolver : DefaultContractResolver
    {
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