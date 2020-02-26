using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Concurrent;

namespace DataWF.WebService.Common
{
    //https://stackoverflow.com/a/36597735
    public class OpenApiCachingProvider : ISwaggerProvider
    {
        private static readonly ConcurrentDictionary<string, OpenApiDocument> _cache =
            new ConcurrentDictionary<string, OpenApiDocument>(StringComparer.Ordinal);

        private readonly ISwaggerProvider _swaggerProvider;

        public OpenApiCachingProvider(ISwaggerProvider swaggerProvider)
        {
            _swaggerProvider = swaggerProvider;
        }

        public OpenApiDocument GetSwagger(string documentName, string host = null, string basePath = null)
        {
            var cacheKey = $"{documentName}_{host}_{basePath}";
            return _cache.GetOrAdd(cacheKey, (key) => _swaggerProvider.GetSwagger(documentName, host, basePath));
        }
    }
}