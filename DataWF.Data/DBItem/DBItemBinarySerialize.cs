//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataWF.Common;

namespace DataWF.Data
{
    public class DBItemServiceConverter<T> : JsonConverter<T> where T : DBItem, new()
    {
        public DBItemServiceConverter()
        { }

        //public override bool CanConvert(Type objectType)
        //{
        //    return TypeHelper.IsBaseType(objectType, typeof(T));
        //}

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var valueType = value.GetType();
            writer.WriteStartObject();
            foreach (var column in value.Table.Columns)
            {
                column.Write(writer, value, options);
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
            var dictionary = new Dictionary<DBColumn, object>();
            var column = (DBColumn)null;
            var propertyName = (string)null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    propertyName = reader.GetString();
                    column = table.ParseColumnProperty(propertyName);
                    if (column == null)
                    {
                        throw new InvalidOperationException($"Property {propertyName} not found!");
                    }
                }
                else
                {
                    dictionary[column] = JsonSerializer.Deserialize(ref reader, column.DataType, options);
                }
            }

            if (table.PrimaryKey != null && dictionary.TryGetValue(table.PrimaryKey, out var value) && value != null)
            {
                item = table.LoadById(value, DBLoadParam.Load | DBLoadParam.Referencing);
            }

            if (item == null)
            {
                if (table.ItemTypeKey != null && dictionary.TryGetValue(table.ItemTypeKey, out var itemType) && itemType != null)
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
