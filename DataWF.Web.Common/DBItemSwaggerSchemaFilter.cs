using DataWF.Common;
using DataWF.Data;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Reflection;

namespace DataWF.Web.Common
{
    public class DBItemSwaggerParameterFilter : IParameterFilter
    {
        public void Apply(IParameter parameter, ParameterFilterContext context)
        {
            if (parameter is NonBodyParameter nonBodyParameter && string.IsNullOrEmpty(nonBodyParameter.Type))
            {
                var type = context.ControllerParameterDescriptor.ParameterType;
                if (type.IsEnum)
                {
                    var schema = context.SchemaRegistry.GetOrRegister(context.ControllerParameterDescriptor.ParameterType);
                    nonBodyParameter.Extensions.Add("schema", schema);
                }
            }
        }
    }

    public class DBItemSwaggerOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            foreach (var parameter in operation.Parameters)
            {
                //if(parameter.)
            }
        }
    }

    public class DBItemSwaggerSchemaFilter : ISchemaFilter
    {
        public void Apply(Schema schema, SchemaFilterContext context)
        {
            if (TypeHelper.IsBaseType(context.SystemType, typeof(DBItem)))
            {
                schema.Properties.Clear();

                //Obsoled as of DBSchema generated required 
                //var table = DBTable.GetTableAttribute(context.SystemType, true);
                //if (table != null)
                //{
                //    var baseSchema = context.SchemaRegistry.GetOrRegister(context.SystemType.BaseType);
                //    schema.AllOf = new List<Schema> { baseSchema };
                //    foreach (var column in table.Columns)
                //    {
                //        if (column.Property != null && column.Property.DeclaringType == context.SystemType)
                //        {
                //            ApplyColumn(schema, context, column);
                //        }
                //    }
                //}

                if (context.SystemType != typeof(DBItem))
                {
                    var baseSchema = context.SchemaRegistry.GetOrRegister(context.SystemType.BaseType);
                    schema.AllOf = new List<Schema> { baseSchema };
                }
                foreach (var property in context.SystemType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
                {
                    var column = DBColumn.GetColumnAttribute(property);
                    if (column != null)
                    {
                        column.Property = property;
                        if (column.DataType == null)
                            column.DataType = property.PropertyType;
                        ApplyColumn(schema, context, column);
                    }
                    var reference = property.GetCustomAttribute<ReferenceAttribute>();
                    if (reference != null)
                    {
                        var referenceSchema = context.SchemaRegistry.GetOrRegister(property.PropertyType);
                        schema.Properties.Add(property.Name, referenceSchema);
                    }
                }
            }
            else
            { }
        }

        public void ApplyColumn(Schema schema, SchemaFilterContext context, ColumnAttribute column)
        {
            var columnSchema = context.SchemaRegistry.GetOrRegister(column.DataType);
            if (column.DataType == typeof(string) && column.Size > 0)
            {
                columnSchema.MaxLength = column.Size;
            }
            if ((column.Keys & DBColumnKeys.System) == DBColumnKeys.System)
            {
                columnSchema.ReadOnly = true;
            }
            if ((column.Keys & DBColumnKeys.Notnull) == DBColumnKeys.Notnull
                || (column.Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary)
            {
                if (schema.Required == null)
                    schema.Required = new List<string>();
                schema.Required.Add(column.Property.Name);
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
        }
    }
}