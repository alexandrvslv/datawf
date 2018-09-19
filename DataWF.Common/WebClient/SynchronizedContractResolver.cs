using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Reflection;

namespace DataWF.Common
{
    public class SynchronizedContractResolver : DefaultContractResolver
    {
        public static readonly SynchronizedContractResolver Instance = new SynchronizedContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (!property.Ignored
                && property.NullValueHandling != null
                && TypeHelper.IsInterface(property.DeclaringType, typeof(ISynchronized))
                && !TypeHelper.IsInterface(property.PropertyType, typeof(IList)))//TODO Check List
            {
                var propertyName = property.PropertyName;
                property.ShouldSerialize =
                    instance =>
                    {
                        var e = (ISynchronized)instance;
                        return e.Changes.Contains(propertyName);
                    };
            }

            return property;
        }

        protected override JsonArrayContract CreateArrayContract(Type objectType)
        {
            var contract = base.CreateArrayContract(objectType);
            contract.Converter = SynchronizedArrayConverter.Instance;
            return contract;
        }
    }
}