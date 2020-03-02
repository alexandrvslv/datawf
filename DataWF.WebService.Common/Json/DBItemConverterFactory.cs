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
        private const string jsonIncludeRef = "json_include_ref";
        private const string jsonReferenceCheck = "json_ref_check";
        private const string jsonMaxDepth = "json_max_depth";
        private HttpContext context;
        private IUserIdentity user;
        private bool? includeReference;
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
            set
            {
                context = value;
                user = context?.User?.GetCommonUser();
            }
        }

        public IUserIdentity CurrentUser
        {
            get => user ?? HttpContext?.User?.GetCommonUser();
            set => user = value;
        }

        public bool IncludeReference
        {
            get => includeReference ?? HttpContext?.ReadBool(jsonIncludeRef) ?? false;
            set => includeReference = value;
        }

        public bool ReferenceCheck
        {
            get => referenceCheck ?? HttpContext?.ReadBool(jsonReferenceCheck) ?? false;
            set => referenceCheck = value;
        }

        public int MaxDepth
        {
            get => maxDepth ?? HttpContext?.ReadInt(jsonMaxDepth) ?? 5;
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
                return (JsonConverter)Activator.CreateInstance(typeof(DBItemConverter<>).MakeGenericType(typeToConvert), this);
            }
            else
            {
                return options.GetConverter(typeToConvert);
            }
        }
    }
}