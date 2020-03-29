﻿/*
 DBRow.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>  

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.WebService.Common
{

    public class DBItemJsonConverter : JsonConverter<DBItem>
    {
        private const string jsonIncludeRef = "json_include_ref";
        private const string jsonReferenceCheck = "json_ref_check";
        private const string jsonMaxDepth = "json_max_depth";
        private IHttpContextAccessor httpContextAccessor;
        private HttpContext context;
        private IUserIdentity user;
        private bool? includeReference;
        private int? maxDepth;
        private bool? referenceCheck;
        private HashSet<DBItem> referenceSet = new HashSet<DBItem>();

        public DBItemJsonConverter()
        {
        }

        public IHttpContextAccessor HttpContextAccessor
        {
            get => httpContextAccessor;
            set => httpContextAccessor = value;
        }

        public HttpContext HttpContext
        {
            get => context ?? HttpContextAccessor?.HttpContext;
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

        public override bool CanConvert(Type objectType)
        {
            return TypeHelper.IsBaseType(objectType, typeof(DBItem));
        }

        public override void Write(Utf8JsonWriter writer, DBItem value, JsonSerializerOptions options)
        {
            bool includeReference = IncludeReference;
            int maxDepth = MaxDepth;
            var valueType = value.GetType();
            var table = DBTable.GetTable(valueType);
            writer.WriteStartObject();

            foreach (var invoker in table.Invokers)
            {
                object propertyValue;
                if (TypeHelper.IsBaseType(invoker.DataType, typeof(DBItem)))
                {
                    if (!includeReference || writer.CurrentDepth > maxDepth)
                        continue;
                    propertyValue = invoker.GetValue(value);
                    if (ReferenceCheck && propertyValue is DBItem reference)
                    {
                        if (referenceSet.Contains(reference))
                            continue;
                        else
                            referenceSet.Add(reference);
                    }
                }
                else
                {
                    propertyValue = invoker.GetValue(value);
                    if (propertyValue is AccessValue accessValue)
                    {
                        propertyValue = accessValue.GetFlags(CurrentUser);
                    }
                }
                writer.WritePropertyName(invoker.Name);
                JsonSerializer.Serialize(writer, propertyValue, options);
            }

            writer.WriteEndObject();
        }

        public override DBItem Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
        {
            var item = (DBItem)null;

            var table = DBTable.GetTable(objectType);
            if (table == null)
            {
                throw new JsonException($"Can't find table of {objectType?.Name ?? "null"}");
            }
            var dictionary = new Dictionary<IInvoker, object>();
            var invoker = (IInvoker)null;
            var propertyName = (string)null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    propertyName = reader.GetString();
                    invoker = table.GetInvoker(propertyName);
                }
                else
                {
                    var proeprtyValue = JsonSerializer.Deserialize(ref reader, invoker.DataType, options);
                    if (invoker != null)
                    {
                        dictionary[invoker] = proeprtyValue;
                    }
                    else if (!options.AllowTrailingCommas)
                    {
                        throw new InvalidOperationException($"Property {propertyName} not found!");
                    }
                }
            }

            if (table.PrimaryKey != null && dictionary.TryGetValue(table.PrimaryKey.PropertyInvoker, out var value) && value != null)
            {
                item = table.LoadItemById(value, DBLoadParam.Load | DBLoadParam.Referencing);
            }

            if (item == null)
            {
                if (table.ItemTypeKey != null && dictionary.TryGetValue(table.ItemTypeKey.PropertyInvoker, out var itemType) && itemType != null)
                {
                    item = table.NewItem(DBUpdateState.Insert, true, (int)itemType);
                }
                else
                {
                    item = table.NewItem(DBUpdateState.Insert, true);
                }
            }

            foreach (var entry in dictionary)
            {
                entry.Key.SetValue(item, entry.Value);
            }
            return item;
        }


    }
}