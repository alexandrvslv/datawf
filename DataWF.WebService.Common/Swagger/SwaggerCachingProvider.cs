using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Concurrent;

namespace DataWF.WebService.Common
{
    //https://stackoverflow.com/a/36597735
    public class SwaggerCachingProvider : ISwaggerProvider
    {
        private static readonly ConcurrentDictionary<string, SwaggerDocument> _cache =
            new ConcurrentDictionary<string, SwaggerDocument>(StringComparer.Ordinal);

        private readonly ISwaggerProvider _swaggerProvider;

        public SwaggerCachingProvider(ISwaggerProvider swaggerProvider)
        {
            _swaggerProvider = swaggerProvider;
        }

        public SwaggerDocument GetSwagger(string documentName, string host = null, string basePath = null, string[] schemes = null)
        {
            var cacheKey = $"{documentName}_{host}_{basePath}";
            return _cache.GetOrAdd(cacheKey, (key) => _swaggerProvider.GetSwagger(documentName, host, basePath, schemes));
        }
    }
}