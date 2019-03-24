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
        public readonly JsonConverter AccessConverter = new AccessValueJsonConverter();
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
                var table = DBTable.GetTableAttributeInherit(objectType);
                if (table != null)
                {
                    var result = new JsonObjectContract(objectType)
                    {
                        Converter = DBItemConverter
                    };

                    foreach (var column in table.Columns.Where(p => TypeHelper.IsBaseType(p.Property.DeclaringType, objectType)))
                    // && (p.Attribute.Keys & DBColumnKeys.System) != DBColumnKeys.System
                    {
                        var jsonProperty = new JsonProperty
                        {
                            DeclaringType = objectType,
                            DefaultValue = column.DefaultValues != null && column.DefaultValues.TryGetValue(objectType, out var defaultValue) ? defaultValue : null,
                            Order = column.Attribute.Order,
                            PropertyName = column.PropertyName,
                            PropertyType = column.Property.PropertyType,
                            ValueProvider = column.PropertyInvoker,
                            Ignored = column.Attribute.ColumnType != DBColumnTypes.Default || (column.Attribute.Keys & DBColumnKeys.Access) == DBColumnKeys.Access
                        };
                        result.Properties.Add(jsonProperty);

                        if (column.ReferenceProperty != null)
                        {
                            jsonProperty = base.CreateProperty(column.ReferenceProperty, MemberSerialization.OptIn);
                            jsonProperty.IsReference = true;
                            jsonProperty.ValueProvider = EmitInvoker.Initialize(column.ReferenceProperty);
                            jsonProperty.NullValueHandling = NullValueHandling.Ignore;
                            result.Properties.Add(jsonProperty);
                        }
                    }

                    return result;
                }
            }
            if (objectType == typeof(AccessValue))
            {
                var result = new JsonObjectContract(objectType)
                {
                    Converter = AccessConverter
                };
                result.Properties.Add(new JsonProperty
                {
                    DeclaringType = objectType,
                    DefaultValue = false,
                    Order = 1,
                    PropertyName = AccessType.View.ToString(),
                    PropertyType = typeof(bool)
                });
                result.Properties.Add(new JsonProperty
                {
                    DeclaringType = objectType,
                    DefaultValue = false,
                    Order = 1,
                    PropertyName = AccessType.Create.ToString(),
                    PropertyType = typeof(bool)
                });
                result.Properties.Add(new JsonProperty
                {
                    DeclaringType = objectType,
                    DefaultValue = false,
                    Order = 1,
                    PropertyName = AccessType.Edit.ToString(),
                    PropertyType = typeof(bool)
                });
                result.Properties.Add(new JsonProperty
                {
                    DeclaringType = objectType,
                    DefaultValue = false,
                    Order = 1,
                    PropertyName = AccessType.Delete.ToString(),
                    PropertyType = typeof(bool)
                });
                result.Properties.Add(new JsonProperty
                {
                    DeclaringType = objectType,
                    DefaultValue = false,
                    Order = 1,
                    PropertyName = AccessType.Admin.ToString(),
                    PropertyType = typeof(bool)
                });
                result.Properties.Add(new JsonProperty
                {
                    DeclaringType = objectType,
                    DefaultValue = false,
                    Order = 1,
                    PropertyName = AccessType.Accept.ToString(),
                    PropertyType = typeof(bool)
                });
                return result;
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