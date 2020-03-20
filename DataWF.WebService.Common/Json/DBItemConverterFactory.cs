using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.WebService.Common
{
    public class DBItemConverterFactory : JsonConverterFactory
    {
        //private readonly Dictionary<Type, JsonConverter> cache = new Dictionary<Type, JsonConverter>();
        private const string jsonIncludeRef = "json_include_ref";
        private const string jsonIncludeRefing = "json_include_refing";
        private const string jsonReferenceCheck = "json_ref_check";
        private const string jsonMaxDepth = "json_max_depth";
        private readonly Type[] types = new Type[] { typeof(DBItemConverterFactory) };
        private HttpContext context;
        private IUserIdentity user;
        private bool? includeReference;
        private bool? includeReferencing;
        private int? maxDepth;
        private bool? referenceCheck;
        internal HashSet<DBItem> referenceSet = new HashSet<DBItem>();

        public DBItemConverterFactory()
        {
        }

        public DBItemConverterFactory(HttpContext context)
        {
            HttpContext = context;
        }

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

        public bool IncludeReference
        {
            get => includeReference ?? (includeReference = HttpContext?.ReadBool(jsonIncludeRef) ?? true).Value;
            set => includeReference = value;
        }

        public bool IncludeReferencing
        {
            get => includeReferencing ?? (includeReferencing = HttpContext?.ReadBool(jsonIncludeRefing) ?? true).Value;
            set => includeReferencing = value;
        }

        public bool ReferenceCheck
        {
            get => referenceCheck ?? (referenceCheck = HttpContext?.ReadBool(jsonReferenceCheck) ?? true).Value;
            set => referenceCheck = value;
        }

        public int MaxDepth
        {
            get => maxDepth ?? (maxDepth = HttpContext?.ReadInt(jsonMaxDepth) ?? 5).Value;
            set => maxDepth = value;
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
    }
}