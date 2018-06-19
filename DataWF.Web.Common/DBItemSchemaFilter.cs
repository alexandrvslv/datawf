using DataWF.Common;
using DataWF.Data;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DataWF.Web.Common
{
    public class DBItemSchemaFilter : ISchemaFilter
    {
        public void Apply(Schema schema, SchemaFilterContext context)
        {
            if (TypeHelper.IsBaseType(context.SystemType, typeof(DBItem)))
            {
                var table = DBTable.GetTable(context.SystemType, null, false, true);
                if (table != null)
                {
                    schema.Properties.Clear();
                    foreach (var column in table.Columns)
                    {
                        if (column.Property != null && (column.Keys & DBColumnKeys.Access) != DBColumnKeys.Access)
                        {
                            schema.Properties.Add(column.Property, context.SchemaRegistry.GetOrRegister(column.DataType));
                        }
                    }
                }
            }
        }
    }
}