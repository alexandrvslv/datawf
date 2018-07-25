using DataWF.Common;
using DataWF.Data;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace DataWF.Web.Common
{

    public class SwaggerDBSchemaFilter : ISchemaFilter
    {
        private Stack<TableAttribute> tables = new Stack<TableAttribute>();

        public void Apply(Schema schema, SchemaFilterContext context)
        {
            if (TypeHelper.IsBaseType(context.SystemType, typeof(DBItem)))
            {
                var temp = DBTable.GetTableAttributeInherit(context.SystemType);
                if (temp != null)
                    tables.Push(temp);

                Schema baseSchema = null;
                if (context.SystemType.BaseType != typeof(object))
                {
                    baseSchema = context.SchemaRegistry.GetOrRegister(context.SystemType.BaseType);
                }
                ApplyTableType(schema, context.SystemType, context, baseSchema);
                if (temp != null)
                    tables.Pop();
            }
        }

        public Schema ApplyTableType(Schema schema, Type type, SchemaFilterContext context, Schema baseSchema)
        {
            var table = tables.Peek();
            if (schema.Properties != null && table != null)
            {
                schema.Properties.Clear();
                foreach (var column in table.Columns.Where(p => p.Property.DeclaringType == type))
                {
                    var columnSchema = context.SchemaRegistry.GetOrRegister(column.GetDataType());
                    ApplyColumn(schema, columnSchema, column);
                    if (column.ReferenceProperty != null)
                    {
                        var referenceSchema = context.SchemaRegistry.GetOrRegister(column.ReferenceProperty.PropertyType);
                        var schemaProperty = new Schema() { Ref = referenceSchema.Ref };
                        schemaProperty.Extensions.Add("x-id", column.PropertyName);
                        schema.Properties.Add(column.ReferenceProperty.Name, schemaProperty);
                    }
                }
            }
            if (baseSchema != null)
                schema.AllOf = new List<Schema> { baseSchema };
            return baseSchema;
        }

        public void ApplyColumn(Schema schema, Schema columnSchema, ColumnAttribute column)
        {
            if ((column.Keys & DBColumnKeys.Access) == DBColumnKeys.Access
                || (column.Keys & DBColumnKeys.Password) == DBColumnKeys.Password)
                return;

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
                schema.Required.Add(column.PropertyName);
            }
            if ((column.Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary)
            {
                schema.Extensions.Add("x-id", column.PropertyName);
            }
            if ((column.Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType)
            {
                schema.Extensions.Add("x-type", column.PropertyName);
            }
            if ((column.Keys & DBColumnKeys.Culture) == DBColumnKeys.Culture)
            {
                foreach (var culture in Locale.Instance.Cultures)
                {
                    schema.Properties.Add(column.PropertyName + culture.TwoLetterISOLanguageName.ToUpper(), columnSchema);
                }
            }
            else
            {
                schema.Properties.Add(column.PropertyName, columnSchema);
            }
            var defaultValue = column.Property.GetCustomAttribute<DefaultValueAttribute>();
            if (defaultValue != null && defaultValue != null)
            {
                columnSchema.Default = defaultValue.Value.ToString();
            }
        }
    }
}