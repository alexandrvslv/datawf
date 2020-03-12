/*
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

    public class DBItemConverter<T> : JsonConverter<T> where T : DBItem, new()
    {

        public DBItemConverter()
        {
        }

        public DBItemConverter(DBItemConverterFactory factory)
        {
            Factory = factory;
        }

        public DBItemConverterFactory Factory { get; set; }

        public override bool CanConvert(Type objectType)
        {
            return TypeHelper.IsBaseType(objectType, typeof(T));
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            bool includeReference = Factory.IncludeReference;
            int maxDepth = Factory.MaxDepth;
            var valueType = value.GetType();
            var table = DBTable.GetTable(valueType);
            writer.WriteStartObject();
            object propertyValue;
            Type propertyType;
            foreach (var invoker in table.Invokers)
            {
                propertyType = invoker.DataType;
                if (TypeHelper.IsBaseType(invoker.DataType, typeof(DBItem)))
                {
                    if (!includeReference || writer.CurrentDepth > maxDepth)
                        continue;
                    propertyValue = invoker.GetValue(value);
                    if (Factory.ReferenceCheck && propertyValue is DBItem reference)
                    {
                        if (Factory.referenceSet.Contains(reference))
                            continue;
                        else
                            Factory.referenceSet.Add(reference);
                    }
                }
                else
                {
                    propertyValue = invoker.GetValue(value);
                    if (propertyValue is AccessValue accessValue)
                    {
                        propertyValue = accessValue.GetFlags(Factory.CurrentUser);
                        propertyType = typeof(AccessType);
                    }
                }
                writer.WritePropertyName(invoker.Name);
                JsonSerializer.Serialize(writer, propertyValue, propertyType, options);
            }

            writer.WriteEndObject();
        }

        public override T Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
        {
            var item = (T)null;

            var table = DBTable.GetTable<T>();
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
                    if (invoker == null)
                    {
                        throw new InvalidOperationException($"Property {propertyName} not found!");
                    }
                }
                else
                {
                    dictionary[invoker] = JsonSerializer.Deserialize(ref reader, invoker.DataType, options);
                }
            }

            if (table.PrimaryKey != null && dictionary.TryGetValue(table.PrimaryKey.PropertyInvoker, out var value) && value != null)
            {
                item = table.LoadById(value, DBLoadParam.Load | DBLoadParam.Referencing);
            }

            if (item == null)
            {
                if (table.ItemTypeKey != null && dictionary.TryGetValue(table.ItemTypeKey.PropertyInvoker, out var itemType) && itemType != null)
                {
                    item = (T)table.NewItem(DBUpdateState.Insert, true, (int)itemType);
                }
                else
                {
                    item = (T)table.NewItem(DBUpdateState.Insert, true);
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