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
        private Stack<TableGenerator> tables = new Stack<TableGenerator>();

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
                Type baseType = context.SystemType.BaseType;
                while (baseType.IsGenericType)
                {
                    baseType = baseType.BaseType;
                }
                if (baseType != typeof(object))
                {
                    baseSchema = context.SchemaRegistry.GetOrRegister(baseType);
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
                ApplyProperties(schema, type, context, table);
                var baseType = type.BaseType;
                while (baseType != null && baseType.IsGenericType)
                {
                    ApplyProperties(schema, baseType, context, table);
                    baseType = baseType.BaseType;
                }
            }
            if (baseSchema != null)
                schema.AllOf = new List<Schema> { baseSchema };
            return baseSchema;
        }

        private void ApplyProperties(Schema schema, Type type, SchemaFilterContext context, TableGenerator table)
        {
            foreach (var column in table.Columns.Where(p => p.PropertyInfo?.GetGetMethod()?.GetBaseDefinition()?.DeclaringType == type))
            {
                var propertyType = TypeHelper.CheckNullable(column.PropertyInfo.PropertyType);
                if (propertyType == typeof(AccessValue))
                {
                    propertyType = typeof(AccessType);
                }
                var columnSchema = context.SchemaRegistry.GetOrRegister(propertyType);
                ApplyColumn(schema, columnSchema, column);
                if (column.ReferencePropertyInfo != null)
                {
                    var referenceSchema = context.SchemaRegistry.GetOrRegister(column.ReferencePropertyInfo.PropertyType);
                    var schemaProperty = new Schema() { Ref = referenceSchema.Ref };
                    schemaProperty.Extensions.Add("x-id", column.PropertyName);
                    schema.Properties.Add(column.ReferencePropertyInfo.Name, schemaProperty);
                }
            }
            if (table.Referencings != null)
            {
                foreach (var refing in table.Referencings.Where(p => p.Property.DeclaringType == type))
                {
                    var refingSchema = context.SchemaRegistry.GetOrRegister(refing.Property.PropertyType);
                    var itemType = TypeHelper.GetItemType(refing.Property.PropertyType);
                    //refingSchema.Extensions.Add("x-ref-client", itemType.Name);
                    refingSchema.Extensions.Add("x-ref-key", refing.ReferenceColumn.PropertyName);
                    schema.Properties.Add(refing.Property.Name, refingSchema);
                }
            }
        }

        public void ApplyColumn(Schema schema, Schema columnSchema, ColumnGenerator column)
        {
            if ((column.Attribute.Keys & DBColumnKeys.Password) == DBColumnKeys.Password
                || (column.Attribute.Keys & DBColumnKeys.File) == DBColumnKeys.File)
                return;

            if (column.GetDataType() == typeof(string) && column.Size > 0)
            {
                columnSchema.MaxLength = column.Size;
            }

            if ((column.Attribute.Keys & DBColumnKeys.Access) == DBColumnKeys.Access
                || (column.Attribute.Keys & DBColumnKeys.Date) == DBColumnKeys.Date
                || (column.Attribute.Keys & DBColumnKeys.Stamp) == DBColumnKeys.Stamp
                || (column.Attribute.Keys & DBColumnKeys.System) == DBColumnKeys.System
                || column.PropertyInfo?.GetSetMethod() == null)
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

            var defaultValue = column.PropertyInfo.GetCustomAttribute<DefaultValueAttribute>();
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
            //if (TypeHelper.GetPassword(column.Property))
            //{
            //    schema.Extensions.Add("x-data", column.PropertyName);
            //}
        }
    }
}