using DataWF.Common;
using DataWF.Data;
using Newtonsoft.Json.Schema;
using NJsonSchema.Generation;
using System;
using System.Linq;

namespace DataWF.Web.Common
{
    public class DBItemSchemaGenerator : NJsonSchema.Generation.JsonSchemaGenerator
    {
        public DBItemSchemaGenerator(JsonSchemaGeneratorSettings settings) : base(settings)
        {
        }

        protected override string[] GetTypeProperties(Type type)
        {
            if (TypeHelper.IsBaseType(type, typeof(DBItem)))
            {
                var table = DBTable.GetTable(type, null, false, true);
                if (table != null)
                {
                    return table.Columns
                        .Where(column => column.Property != null && column.Access.View && (column.Keys & DBColumnKeys.Access) != DBColumnKeys.Access)
                        .Select(column => column.Property).ToArray();
                }
            }
            return base.GetTypeProperties(type);
        }
    }
}