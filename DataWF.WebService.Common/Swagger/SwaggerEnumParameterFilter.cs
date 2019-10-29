using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DataWF.WebService.Common
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
}