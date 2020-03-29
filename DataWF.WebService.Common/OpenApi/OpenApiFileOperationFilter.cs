using DataWF.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataWF.WebService.Common
{
    //http://www.talkingdotnet.com/how-to-upload-file-via-swagger-in-asp-net-core-web-api/
    public class OpenApiFileOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.MethodInfo.CustomAttributes.Any(p => p.AttributeType == typeof(DisableFormValueModelBindingAttribute)))
            {
                operation.RequestBody = GenFileResponce();
            }

            if (context.ApiDescription.SupportedResponseTypes.Any(p =>
            {
                return TypeHelper.IsBaseType(p.Type, typeof(Stream))
                || TypeHelper.IsBaseType(TypeHelper.GetItemType(p.Type), typeof(Stream))
                || TypeHelper.IsBaseType(p.Type, typeof(FileStreamResult))
                || TypeHelper.IsBaseType(TypeHelper.GetItemType(p.Type), typeof(FileStreamResult));

            }))
            {
                operation.Responses["200"] = new OpenApiResponse
                {
                    Description = "file to been returned",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        { "application/octet-stream",
                            new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format="binary"
                                }
                            }
                        }
                    }

                };
            }
            if (operation.Responses.TryGetValue("200", out var openApiResponse)
                && (openApiResponse.Content?.TryGetValue("application/json", out var responceApiMediaType) ?? false))
            {
                openApiResponse.Content.Remove("text/json");
                openApiResponse.Content.Remove("text/plain");
            }
            if ((operation.RequestBody?.Content?.TryGetValue("application/json", out var bodyApiMediaType) ?? false))
            {
                operation.RequestBody.Content.Remove("text/json");
                operation.RequestBody?.Content.Remove("application/*+json");
            }
        }

        private static OpenApiRequestBody GenFileResponce()
        {
            return new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                        { "multipart/form-data",
                            new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Type = "object",
                                    Properties = new Dictionary<string, OpenApiSchema>
                                    {
                                        { "file",
                                            new OpenApiSchema
                                            {
                                                Type="array",
                                                Items = new OpenApiSchema
                                                {
                                                    Type="string",
                                                    Format="binary"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                }
            };
        }
    }
}