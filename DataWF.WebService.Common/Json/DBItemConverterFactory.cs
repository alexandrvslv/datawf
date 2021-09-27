using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.WebService.Common
{
    public class DBItemConverterFactory : JsonConverterFactory, IDisposable
    {
        //private readonly Dictionary<Type, JsonConverter> cache = new Dictionary<Type, JsonConverter>();

        private static readonly Type[] types = new Type[] { typeof(DBItemConverterFactory) };
        private HttpContext context;
        private IUserIdentity user;
        private HttpJsonSettings? httpJsonSettings;

        internal HashSet<DBItem> referenceSet = new HashSet<DBItem>();

        public DBItemConverterFactory(IDBProvider provider)
        {
            Provider = provider;
        }

        public DBItemConverterFactory(HttpContext context, IDBProvider provider)
            : this(provider)
        {
            HttpContext = context;
        }

        public IDBProvider Provider { get; set; }

        public HttpContext HttpContext
        {
            get => context;
            set => context = value;
        }

        public IUserIdentity CurrentUser
        {
            get => user ?? (user = HttpContext?.User?.GetCommonUser());
            set => user = value;
        }
        public HttpJsonSettings HttpJsonSettings
        {
            get => httpJsonSettings ?? (httpJsonSettings = HttpContext?.ReadJsonSettings() ?? HttpJsonSettings.Default).Value;
            set => httpJsonSettings = value;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return TypeHelper.IsBaseType(typeToConvert, typeof(DBItem));
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (TypeHelper.IsBaseType(typeToConvert, typeof(DBItem)))
            {
                //if (!cache.TryGetValue(typeToConvert, out var converter))
                //{
                //    cache[typeToConvert] = converter = ;
                //}
                return (JsonConverter)EmitInvoker.CreateObject(typeof(DBItemConverter<>).MakeGenericType(typeToConvert), types, new[] { this }, true);
            }
            else
            {
                return options.GetConverter(typeToConvert);
            }
        }

        public void Dispose()
        {
            referenceSet?.Clear();
            referenceSet = null;
            context = null;
            user = null;
        }
    }
}