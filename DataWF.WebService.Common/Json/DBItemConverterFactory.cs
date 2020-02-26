using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Http;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.WebService.Common
{
    public class DBItemConverterFactory : JsonConverterFactory
    {
        public DBItemConverterFactory(IHttpContextAccessor accessor)
        {
            Accessor = accessor;
        }

        public IHttpContextAccessor Accessor { get; }

        public override bool CanConvert(Type typeToConvert)
        {
            return TypeHelper.IsBaseType(typeToConvert, typeof(DBItem));
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (TypeHelper.IsBaseType(typeToConvert, typeof(DBItem)))
            {
                return new DBItemJsonConverter() { HttpContextAccessor = Accessor };
            }
            else
            {
                return options.GetConverter(typeToConvert);
            }
        }
    }
}