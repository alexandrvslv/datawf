using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;

namespace DataWF.WebService.Common
{
    public class SwaggerSecurityDocumentFilter : IDocumentFilter
    {
        public void Apply(SwaggerDocument document, DocumentFilterContext context)
        {
            document.Security = new List<IDictionary<string, IEnumerable<string>>>()
            {
                new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
                {
                    { "Bearer", new string[]{ } },
                }
            };
        }
    }
}