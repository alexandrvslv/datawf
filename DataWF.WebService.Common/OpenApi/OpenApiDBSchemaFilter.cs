using DataWF.Common;
using DataWF.Data;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace DataWF.WebService.Common
{
    public class OpenApiDBSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (!TypeHelper.IsSerializeAttribute(context.Type)
                && !TypeHelper.IsEnumerable(context.Type)
                && context.Type != typeof(object))
            {
                schema.Type = "object";
                schema.Properties.Clear();
                schema.AdditionalPropertiesAllowed = true;

                OpenApiSchema baseSchema = GetBaseSchema(context);
                if (baseSchema != null)
                {
                    schema.AllOf = new List<OpenApiSchema> { baseSchema };
                }

                if (TypeHelper.IsBaseType(context.Type, typeof(DBItem)))
                {
                    var table = TableGenerator.GetInherit(context.Type);
                    if (table != null)
                    {
                        ApplyTable(schema, context, table);
                        return;
                    }
                }
                ApplyObject(schema, context);
            }
            else if (context.Type.IsEnum)
            {
                var items = EnumItem.GetEnumItems(context.Type);

                var namesArray = new OpenApiArray();
                namesArray.AddRange(items.Select(p => new OpenApiString(p.Name)));
                schema.Extensions.Add("x-enumNames", namesArray);

                var textArray = new OpenApiArray();
                textArray.AddRange(items.Select(p => new OpenApiString(p.Text)));
                schema.Extensions.Add("x-enumMembers", textArray);

                if (context.Type.GetCustomAttribute<FlagsAttribute>() != null)
                    schema.Extensions.Add("x-flags", new OpenApiInteger(Enum.GetValues(context.Type).Cast<int>().First()));
            }

        }

        private OpenApiSchema GetBaseSchema(SchemaFilterContext context)
        {
            Type baseType = context.Type.BaseType;
            if (baseType == null)
                return null;
            OpenApiSchema baseSchema = null;
            while (baseType.IsGenericType)
            {
                baseType = baseType.BaseType;
            }
            if (baseType != typeof(object) && baseType != typeof(ValueType))
            {
                baseSchema = context.SchemaGenerator.GenerateSchema(baseType, context.SchemaRepository);
                //context.SchemaRepository.GetOrAdd(baseType,
                //    context.SchemaRepository.TryGetIdFor(baseType, out var baseId) ? baseId : baseType.Name,
                //    () =>
                //    {
                //        var newSchema = new OpenApiSchema { Type = "object" }; //;
                //        this.Apply(newSchema, new SchemaFilterContext(baseType, context.SchemaRepository, context.SchemaGenerator));
                //        return newSchema;
                //    });
            }

            return baseSchema;
        }

        private void ApplyObject(OpenApiSchema schema, SchemaFilterContext context)
        {
            ApplyObjectProperties(schema, context.Type, context);
            var baseType = context.Type.BaseType;
            while (baseType != null && baseType.IsGenericType)
            {
                ApplyObjectProperties(schema, baseType, context);
                baseType = baseType.BaseType;
            }
        }

        private static void ApplyObjectProperties(OpenApiSchema schema, Type type, SchemaFilterContext context)
        {
            var info = Serialization.Instance.GetTypeInfo(context.Type);
            foreach (var property in info.Properties.Where(p => p.PropertyInfo?.GetGetMethod()?.GetBaseDefinition()?.DeclaringType == context.Type))
            {
                var propertyType = TypeHelper.CheckNullable(property.DataType);
                var columnAttribute = property.PropertyInfo.GetCustomAttribute<ColumnAttribute>()
                    ?? property.PropertyInfo.GetCustomAttribute<LogColumnAttribute>();
                var referenceAttribute = property.PropertyInfo.GetCustomAttribute<ReferenceAttribute>();
                if (columnAttribute != null
                    && (columnAttribute.Keys & DBColumnKeys.Access) != 0
                    && propertyType == typeof(AccessValue))
                {
                    propertyType = typeof(AccessType);
                }
                var propertySchema = context.SchemaGenerator.GenerateSchema(propertyType, context.SchemaRepository);
                if (propertySchema.Reference != null)
                {
                    propertySchema = new OpenApiSchema
                    {
                        AllOf = new List<OpenApiSchema> { propertySchema },
                    };
                }
                if (propertyType != property.DataType)
                {
                    propertySchema.Nullable = true;
                }
                if (columnAttribute != null)
                {
                    ApplyColumnAttribute(schema, propertySchema, columnAttribute, property.PropertyInfo);
                }
                else
                {
                    if (property.IsReadOnly)
                    {
                        propertySchema.ReadOnly = true;
                    }
                    if (property.IsRequired)
                    {
                        schema.Required.Add(property.Name);
                    }
                }
                if (referenceAttribute != null)
                {
                    propertySchema.Extensions.Add("x-id", new OpenApiString(referenceAttribute.ColumnProperty));
                }
                if (property.Default != null)
                {
                    ApplyDefault(propertySchema, property.Default);
                }

                schema.Properties.Add(property.Name, propertySchema);
            }
        }

        public void ApplyTable(OpenApiSchema schema, SchemaFilterContext context, TableGenerator table)
        {
            ApplyTableProperties(schema, context.Type, context, table);
            var baseType = context.Type.BaseType;

            while (baseType?.IsGenericType ?? false)
            {
                ApplyTableProperties(schema, baseType, context, table);
                baseType = baseType.BaseType;
            }

            if (!context.Type.IsAbstract)
            {
                var itemType = TableGenerator.GetItemType(context.Type);
                schema.Extensions.Add("x-type-id", new OpenApiInteger(itemType?.Attribute.Id ?? 0));
                if (context.Type == table.ItemType)
                {
                    var mapping = new Dictionary<string, string>();
                    foreach (var type in table.Types)
                    {
                        var itemTypeAttribute = TableGenerator.GetItemType(type);
                        if (itemTypeAttribute != null)
                        {
                            var itemTypeSchema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                            mapping.Add(itemTypeAttribute.Attribute.Id.ToString(), itemTypeSchema.Reference.ReferenceV3);
                        }
                    }
                    if (mapping.Count > 0)
                    {
                        schema.Discriminator = new OpenApiDiscriminator
                        {
                            PropertyName = table.TypeKey.PropertyName
                        };
                        schema.Discriminator.Mapping["0"] = $"#/components/schemas/{context.Type.Name}";
                        foreach (var mapItem in mapping)
                        {
                            schema.Discriminator.Mapping[mapItem.Key] = mapItem.Value;
                        }
                    }
                }
            }
            else
            {
                schema.Extensions.Add("x-type-id", new OpenApiInteger(-1));
            }
        }

        private void ApplyTableProperties(OpenApiSchema schema, Type type, SchemaFilterContext context, TableGenerator table)
        {
            foreach (var column in table.Columns.Where(p => p.PropertyInfo?.GetGetMethod()?.GetBaseDefinition()?.DeclaringType == type
                                                         && !TypeHelper.IsNonSerialize(p.PropertyInfo)))
            {
                var propertyType = TypeHelper.CheckNullable(column.PropertyInfo.PropertyType);
                if ((column.Attribute.Keys & DBColumnKeys.Access) != 0
                    && propertyType == typeof(AccessValue))
                {
                    propertyType = typeof(AccessType);
                }
                var columnSchema = context.SchemaGenerator.GenerateSchema(propertyType, context.SchemaRepository);
                if (columnSchema.Reference != null)
                {
                    columnSchema = new OpenApiSchema
                    {
                        AllOf = new List<OpenApiSchema> { columnSchema },
                    };
                }
                if (propertyType != column.PropertyInfo.PropertyType)
                {
                    columnSchema.Nullable = true;
                }
                ApplyColumn(schema, columnSchema, column);
                if (column.ReferencePropertyInfo != null)
                {
                    GenerateReferenceProperty(schema, context, column);
                }
            }
            foreach (var column in table.Columns.Where(p => p.ReferencePropertyInfo?.GetGetMethod()?.GetBaseDefinition().DeclaringType == type
                                                         && p.PropertyInfo?.GetGetMethod()?.GetBaseDefinition()?.DeclaringType != type
                                                         && !TypeHelper.IsNonSerialize(p.ReferencePropertyInfo)))
            {
                GenerateReferenceProperty(schema, context, column);
            }
            if (table.Referencings != null)
            {
                foreach (var refing in table.Referencings.Where(p => p.PropertyInfo.DeclaringType == type
                                                                  && !TypeHelper.IsNonSerialize(p.PropertyInfo)))
                {
                    var refingPropertyType = refing.PropertyInfo.PropertyType;
                    var itemType = TypeHelper.GetItemType(refing.PropertyInfo.PropertyType);
                    var refingSchema = context.SchemaGenerator.GenerateSchema(refingPropertyType, context.SchemaRepository);
                    //refingSchema.Extensions.Add("x-ref-client", itemType.Name);
                    refingSchema.Extensions.Add("x-ref-key", new OpenApiString(refing.ReferenceColumn.PropertyName));
                    schema.Properties.Add(refing.PropertyInfo.Name, refingSchema);
                }
            }
        }

        private static void GenerateReferenceProperty(OpenApiSchema schema, SchemaFilterContext context, ColumnGenerator column)
        {
            var refPropertyType = column.ReferencePropertyInfo.PropertyType;
            var referenceSchema = context.SchemaGenerator.GenerateSchema(refPropertyType, context.SchemaRepository);
            var propertySchema = new OpenApiSchema() { AllOf = new List<OpenApiSchema> { referenceSchema } };
            propertySchema.Extensions.Add("x-id", new OpenApiString(column.PropertyName));
            schema.Properties.Add(column.ReferencePropertyInfo.Name, propertySchema);
        }

        public void ApplyColumn(OpenApiSchema schema, OpenApiSchema columnSchema, ColumnGenerator column)
        {
            if ((column.Attribute.Keys & DBColumnKeys.Password) == DBColumnKeys.Password
                || (column.Attribute.Keys & DBColumnKeys.File) == DBColumnKeys.File)
                return;

            ApplyColumnAttribute(schema, columnSchema, column.Attribute, column.PropertyInfo);
            if (column.Culture != null)
            {
                columnSchema.Extensions.Add("x-culture", new OpenApiString(column.Culture.ToString()));
            }

            var defaultValue = column.PropertyInfo.GetCustomAttribute<DefaultValueAttribute>();
            if (defaultValue != null && defaultValue.Value != null)
            {
                ApplyDefault(columnSchema, defaultValue.Value);
            }

            schema.Properties.Add(column.PropertyName, columnSchema);

            //if (TypeHelper.GetPassword(column.Property))
            //{
            //    schema.Extensions.Add("x-data", column.PropertyName);
            //}
        }

        private static void ApplyColumnAttribute(OpenApiSchema schema, OpenApiSchema property, ColumnAttribute column, PropertyInfo propertyInfo)
        {
            if (propertyInfo?.PropertyType == typeof(string) && column.Size > 0)
            {
                property.MaxLength = column.Size;
            }
            if ((column.Keys & DBColumnKeys.Access) == DBColumnKeys.Access
                || (column.Keys & DBColumnKeys.Date) == DBColumnKeys.Date
                || (column.Keys & DBColumnKeys.Stamp) == DBColumnKeys.Stamp
                || (column.Keys & DBColumnKeys.System) == DBColumnKeys.System
                || propertyInfo?.GetSetMethod() == null)
            {
                property.ReadOnly = true;
            }
            if (((column.Keys & DBColumnKeys.Notnull) == DBColumnKeys.Notnull
                || (column.Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType)
                || (column.Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary)
            {
                if (schema.Required == null)
                    schema.Required = new HashSet<string>();
                schema.Required.Add(propertyInfo.Name);
            }
            if ((column.Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary)
            {
                schema.Extensions.Add("x-id", new OpenApiString(propertyInfo.Name));
            }
            if ((column.Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType)
            {
                schema.Extensions.Add("x-type", new OpenApiString(propertyInfo.Name));
            }
        }

        private static void ApplyDefault(OpenApiSchema schema, object defaultValue)
        {
            if (defaultValue.GetType().IsEnum)
            {
                schema.Default = new OpenApiString(EnumItem.Format(defaultValue));
            }
            else
            {
                schema.Default = new OpenApiString(defaultValue.ToString());
            }
        }
    }
}