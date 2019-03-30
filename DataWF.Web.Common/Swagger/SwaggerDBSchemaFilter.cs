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
        private Stack<TableAttributeCache> tables = new Stack<TableAttributeCache>();

        public void Apply(Schema schema, SchemaFilterContext context)
        {
            if (TypeHelper.IsBaseType(context.SystemType, typeof(DBItem)))
            {
                var temp = DBTable.GetTableAttributeInherit(context.SystemType);
                if (temp != null)
                {
                    tables.Push(temp);
                }

                Schema baseSchema = null;
                if (context.SystemType.BaseType != typeof(object))
                {
                    baseSchema = context.SchemaRegistry.GetOrRegister(context.SystemType.BaseType);
                }
                ApplyTableType(schema, context.SystemType, context, baseSchema);

                if (context.SystemType != typeof(DBItem) && context.SystemType != typeof(DBGroupItem))
                {
                    var itemType = DBTable.GetItemTypeAttribute(context.SystemType);
                    schema.Extensions.Add("x-type-id", itemType?.Attribute.Id ?? 0);
                }
                if (temp != null)
                {
                    tables.Pop();
                }
            }
            if (context.SystemType.IsEnum && context.SystemType.GetCustomAttribute<FlagsAttribute>() != null)
            {
                schema.Extensions.Add("x-flags", Enum.GetValues(context.SystemType).Cast<int>().First());
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
                    var propertyType = TypeHelper.CheckNullable(column.Property.PropertyType);
                    if (propertyType == typeof(AccessValue))
                    {
                        propertyType = typeof(AccessType);
                    }
                    var columnSchema = context.SchemaRegistry.GetOrRegister(propertyType);
                    ApplyColumn(schema, columnSchema, column);
                    if (column.ReferenceProperty != null)
                    {
                        var referenceSchema = context.SchemaRegistry.GetOrRegister(column.ReferenceProperty.PropertyType);
                        var schemaProperty = new Schema() { Ref = referenceSchema.Ref };
                        schemaProperty.Extensions.Add("x-id", column.PropertyName);
                        schema.Properties.Add(column.ReferenceProperty.Name, schemaProperty);
                    }
                }
                foreach (var refing in table.Referencings.Where(p => p.Property.DeclaringType == type))
                {
                    var refingSchema = context.SchemaRegistry.GetOrRegister(refing.Property.PropertyType);
                    //var schemaProperty = new Schema() { Ref = referenceSchema.Ref };
                    schema.Properties.Add(refing.Property.Name, refingSchema);
                }
            }
            if (baseSchema != null)
                schema.AllOf = new List<Schema> { baseSchema };
            return baseSchema;
        }

        public void ApplyColumn(Schema schema, Schema columnSchema, ColumnAttributeCache column)
        {
            if ((column.Attribute.Keys & DBColumnKeys.Password) == DBColumnKeys.Password
                || (column.Attribute.Keys & DBColumnKeys.File) == DBColumnKeys.File
                || (column.Attribute.Keys & DBColumnKeys.FileLOB) == DBColumnKeys.FileLOB)
                return;

            if (column.GetDataType() == typeof(string) && column.Attribute.Size > 0)
            {
                columnSchema.MaxLength = column.Attribute.Size;
            }

            if ((column.Attribute.Keys & DBColumnKeys.Access) == DBColumnKeys.Access
                || (column.Attribute.Keys & DBColumnKeys.Date) == DBColumnKeys.Date
                || (column.Attribute.Keys & DBColumnKeys.Stamp) == DBColumnKeys.Stamp
                || column.Property.GetSetMethod() == null)
            {
                columnSchema.ReadOnly = true;
            }
            if (((column.Attribute.Keys & DBColumnKeys.Notnull) == DBColumnKeys.Notnull
                || (column.Attribute.Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType)
                || (column.Attribute.Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary)
            {
                if (schema.Required == null)
                    schema.Required = new List<string>();
                schema.Required.Add(column.PropertyName);
            }
            if ((column.Attribute.Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary)
            {
                schema.Extensions.Add("x-id", column.PropertyName);
            }
            if ((column.Attribute.Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType)
            {
                schema.Extensions.Add("x-type", column.PropertyName);
            }
            schema.Properties.Add(column.PropertyName, columnSchema);

            var defaultValue = column.Property.GetCustomAttribute<DefaultValueAttribute>();
            if (defaultValue != null && defaultValue.Value != null)
            {
                if (defaultValue.Value.GetType().IsEnum)
                {
                    columnSchema.Default = EnumItem.Format(defaultValue.Value);
                }
                else
                {
                    columnSchema.Default = defaultValue.Value.ToString();
                }
            }
        }
    }
}