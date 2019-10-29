using DataWF.Common;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.IO;
using System.Linq;

namespace DataWF.WebService.Common
{
    //http://www.talkingdotnet.com/how-to-upload-file-via-swagger-in-asp-net-core-web-api/
    public class SwaggerFileUploadOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (context.MethodInfo.CustomAttributes.Any(p =>
            {
                return p.AttributeType == typeof(DisableFormValueModelBindingAttribute);
            }))
            {
                operation.Consumes.Add("multipart/form-data");
                operation.Parameters.Add(new NonBodyParameter
                {
                    Name = "file",
                    In = "formData",
                    Description = "Upload File",
                    Required = true,
                    Type = "file"
                });
            }
            else if (context.ApiDescription.SupportedResponseTypes.Any(p =>
            {
                return TypeHelper.IsBaseType(p.Type, typeof(Stream))
                || TypeHelper.IsBaseType(TypeHelper.GetItemType(p.Type), typeof(Stream))
                || TypeHelper.IsBaseType(p.Type, typeof(FileStreamResult))
                || TypeHelper.IsBaseType(TypeHelper.GetItemType(p.Type), typeof(FileStreamResult));

            }))
            {
                operation.Produces = new string[] { System.Net.Mime.MediaTypeNames.Application.Octet };
                operation.Responses["200"].Schema = new Schema { Type = "file" };
            }
        }
    }
}