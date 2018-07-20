using DataWF.Common;
using DataWF.Data;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace DataWF.Web.Common
{
    public class SwaggerEnumParameterFilter : IParameterFilter
    {
        public void Apply(IParameter parameter, ParameterFilterContext context)
        {
            if (parameter is NonBodyParameter nonBodyParameter && string.IsNullOrEmpty(nonBodyParameter.Type))
            {
                var type = context.ApiParameterDescription.Type;
                if (type.IsEnum)
                {
                    nonBodyParameter.Type = "string";
                    var schema = context.SchemaRegistry.GetOrRegister(context.ApiParameterDescription.Type);
                    nonBodyParameter.Extensions.Add("schema", schema);
                }
            }
        }
    }

    public class SwaggerSecurityDocumentFilter : IDocumentFilter
    {
        public void Apply(SwaggerDocument document, DocumentFilterContext context)
        {
            document.Security = new List<IDictionary<string, IEnumerable<string>>>()
            {
                new Dictionary<string, IEnumerable<string>>()
                {
                    { "Bearer", new string[]{ } },
                }
            };
        }
    }

    public class SwaggerDBTableSchemaFilter : ISchemaFilter
    {
        public void Apply(Schema schema, SchemaFilterContext context)
        {
            if (TypeHelper.IsBaseType(context.SystemType, typeof(DBItem)))
            {
                schema.Properties.Clear();

                if (context.SystemType != typeof(DBItem))
                {
                    var baseSchema = context.SchemaRegistry.GetOrRegister(context.SystemType.BaseType);
                    schema.AllOf = new List<Schema> { baseSchema };
                }
                foreach (var property in context.SystemType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
                {
                    var column = property.GetCustomAttribute<ColumnAttribute>(false);
                    if (column != null)
                    {
                        column.Property = property;
                        if (column.DataType == null)
                            column.DataType = property.PropertyType;
                        ApplyColumn(schema, context, column);
                    }
                    var reference = property.GetCustomAttribute<ReferenceAttribute>(false);
                    if (reference != null)
                    {
                        var referenceSchema = context.SchemaRegistry.GetOrRegister(property.PropertyType);
                        var schemaProperty = new Schema() { Ref = referenceSchema.Ref };
                        schemaProperty.Extensions.Add("x-id", reference.ColumnProperty);
                        schema.Properties.Add(property.Name, schemaProperty);

                    }
                }
            }
        }

        public void ApplyColumn(Schema schema, SchemaFilterContext context, ColumnAttribute column)
        {
            if ((column.Keys & DBColumnKeys.Access) == DBColumnKeys.Access
                || (column.Keys & DBColumnKeys.Password) == DBColumnKeys.Password)
                return;
            var columnSchema = context.SchemaRegistry.GetOrRegister(column.GetDataType());
            if (column.DataType == typeof(string) && column.Size > 0)
            {
                columnSchema.MaxLength = column.Size;
            }
            if ((column.Keys & DBColumnKeys.System) == DBColumnKeys.System
                || column.Property.GetSetMethod() == null)
            {
                columnSchema.ReadOnly = true;
            }
            if ((column.Keys & DBColumnKeys.Notnull) == DBColumnKeys.Notnull
                && (column.Keys & DBColumnKeys.Primary) != DBColumnKeys.Primary
                && (column.Keys & DBColumnKeys.System) != DBColumnKeys.System)
            {
                if (schema.Required == null)
                    schema.Required = new List<string>();
                schema.Required.Add(column.Property.Name);
            }
            if ((column.Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary)
            {
                schema.Extensions.Add("x-id", column.Property.Name);
            }
            if ((column.Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType)
            {
                schema.Extensions.Add("x-type", column.Property.Name);
            }
            if ((column.Keys & DBColumnKeys.Culture) == DBColumnKeys.Culture)
            {
                foreach (var culture in Locale.Instance.Cultures)
                {
                    schema.Properties.Add(column.Property.Name + culture.TwoLetterISOLanguageName.ToUpper(), columnSchema);
                }
            }
            else
            {
                schema.Properties.Add(column.Property.Name, columnSchema);
            }
            var defaultValue = column.Property.GetCustomAttribute<DefaultValueAttribute>();
            if (defaultValue != null && defaultValue != null)
            {
                columnSchema.Default = defaultValue.Value.ToString();
            }
        }
    }
}