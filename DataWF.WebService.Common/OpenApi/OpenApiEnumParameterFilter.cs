using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DataWF.WebService.Common
{
    public class OpenApiEnumParameterFilter : IParameterFilter
    {
        public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
        {
            if (parameter.In != null && parameter.Schema == null)
            {
                var type = context.ApiParameterDescription.Type;
                if (type.IsEnum)
                {
                    parameter.Schema = context.SchemaRepository.GetOrAdd(context.ApiParameterDescription.Type,
                        context.ApiParameterDescription.Type.Name,
                        () => context.SchemaGenerator.GenerateSchema(context.ApiParameterDescription.Type, context.SchemaRepository));
                }
            }
        }
    }
}