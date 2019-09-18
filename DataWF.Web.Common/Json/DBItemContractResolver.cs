using DataWF.Common;
using DataWF.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;

namespace DataWF.Web.Common
{
    public class DBItemContractResolver : DefaultContractResolver
    {
        public static readonly DBItemContractResolver Instance = new DBItemContractResolver();
        public readonly JsonConverter DBItemConverter = new DBItemJsonConverter();
        public readonly JsonConverter StringConverter = new StringEnumConverter();

        public DBItemContractResolver()
        { }

        public override JsonContract ResolveContract(Type type)
        {
            var contract = base.ResolveContract(type);
            return contract;
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            if (TypeHelper.IsBaseType(objectType, typeof(DBItem)))
            {
                var table = DBTable.GetTable(objectType);
                if (table != null)
                {
                    var result = new JsonObjectContract(objectType)
                    {
                        Converter = DBItemConverter
                    };

                    foreach (var column in table.Columns)
                    {
                        if (column.PropertyInfo == null
                            || !TypeHelper.IsBaseType(objectType, column.PropertyInfo.DeclaringType))
                        {
                            continue;
                        }
                        if (column.PropertyInfo.PropertyType == typeof(AccessValue))
                        {
                            var accessProperty = new JsonProperty
                            {
                                DeclaringType = objectType,
                                DefaultValue = null,
                                Order = column.Order,
                                PropertyName = column.Property,
                                PropertyType = typeof(AccessType?),
                                ValueProvider = column.PropertyInvoker,
                                Ignored = true
                            };
                            result.Properties.Add(accessProperty);
                            continue;
                        }
                        var jsonProperty = new JsonProperty
                        {
                            DeclaringType = objectType,
                            DefaultValue = column.DefaultValues != null && column.DefaultValues.TryGetValue(objectType, out var defaultValue)
                                  ? defaultValue : null,
                            Order = column.Order,
                            PropertyName = column.Property,
                            PropertyType = column.PropertyInfo.PropertyType,
                            ValueProvider = column.PropertyInvoker,
                            Ignored = column.ColumnType != DBColumnTypes.Default || (column.Keys & DBColumnKeys.Access) == DBColumnKeys.Access
                        };
                        result.Properties.Add(jsonProperty);

                        if (column.ReferencePropertyInfo != null)
                        {
                            jsonProperty = base.CreateProperty(column.ReferencePropertyInfo, MemberSerialization.OptIn);
                            jsonProperty.IsReference = true;
                            jsonProperty.ValueProvider = column.ReferencePropertyInvoker;
                            jsonProperty.NullValueHandling = NullValueHandling.Ignore;
                            result.Properties.Add(jsonProperty);
                        }
                    }

                    return result;
                }
            }

            var contract = base.CreateObjectContract(objectType);
            if (objectType.IsEnum)
            {
                contract.Converter = StringConverter;
            }
            return contract;
        }
    }
}