using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;
using System.Reflection;

namespace DataWF.Web.Common
{
    //http://www.talkingdotnet.com/how-to-upload-file-via-swagger-in-asp-net-core-web-api/
    public class SwaggerFileUploadOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var formAttribute = context.MethodInfo.GetCustomAttribute<DisableFormValueModelBindingAttribute>();
            if (formAttribute != null)
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
        }
    }
}