using DataWF.Common;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DataWF.WebService.Common
{
    public class OpenApiEnumParameterFilter : IParameterFilter
    {
        public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
        {
            if (parameter.In != null)
            {
                if (parameter.Schema == null)
                {
                    var type = context.ApiParameterDescription.Type;
                    if (type.IsEnum && !context.SchemaRepository.Schemas.ContainsKey(context.ApiParameterDescription.Type.Name))
                    {
                        parameter.Schema = context.SchemaRepository.AddDefinition(context.ApiParameterDescription.Type.Name,
                             context.SchemaGenerator.GenerateSchema(context.ApiParameterDescription.Type, context.SchemaRepository));
                    }
                }
                else if (context.ApiParameterDescription.Type.IsNullable())
                {
                    parameter.Schema.Nullable = true;
                }
            }
        }
    }
}