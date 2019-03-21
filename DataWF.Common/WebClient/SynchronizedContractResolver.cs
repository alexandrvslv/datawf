using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace DataWF.Common
{
    public class SynchronizedContractResolver : DefaultContractResolver
    {
        public static readonly SynchronizedContractResolver Instance = new SynchronizedContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (property.Ignored)
            {
                return property;
            }

            if (property.NullValueHandling != null)
            {
                if (TypeHelper.IsInterface(property.DeclaringType, typeof(ISynchronized)))
                {
                    var propertyName = property.PropertyName;
                    if (!TypeHelper.IsEnumerable(property.PropertyType))
                    {
                        property.ShouldSerialize =
                            instance =>
                            {
                                var e = (ISynchronized)instance;
                                return e.Changes.ContainsKey(propertyName);
                            };
                    }
                    else if (TypeHelper.IsInterface(TypeHelper.GetItemType(property.PropertyType), typeof(ISynchronized)))
                    {
                        var propertyInvoker = EmitInvoker.Initialize(property.DeclaringType, propertyName);
                        property.ShouldSerialize =
                            instance =>
                            {
                                var collection = (IEnumerable)propertyInvoker.GetValue(instance);
                                return collection != null && collection.TypeOf<ISynchronized>().Any(p => p.SyncStatus != SynchronizedStatus.Actual);
                            };
                    }
                }
            }
            else if (member.GetCustomAttribute<JsonIgnoreSerializationAttribute>() != null)
            {
                property.ShouldSerialize = instance => false;
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