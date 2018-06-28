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

using System;
using System.Collections.Generic;
using DataWF.Common;
using DataWF.Data;
using Newtonsoft.Json;

namespace DataWF.Web.Common
{
    public class DBItemJsonConverter : JsonConverter
    {
        public bool IsSerializeableColumn(DBColumn column)
        {
            return column.PropertyInvoker != null
                && (column.Keys & DBColumnKeys.Access) != DBColumnKeys.Access
                && (column.Keys & DBColumnKeys.Password) != DBColumnKeys.Password;
        }

        public override bool CanConvert(Type objectType)
        {
            return TypeHelper.IsBaseType(objectType, typeof(DBItem));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is DBItem item))
            {
                throw new JsonSerializationException($"Expect {nameof(DBItem)} but {nameof(value)} is {value?.GetType().Name ?? "null"}");
            }
            writer.WriteStartObject();
            var table = item.Table;
            var valueType = value.GetType();
            foreach (var column in table.Columns)
            {
                if (!IsSerializeableColumn(column))
                    continue;
                if (TypeHelper.IsBaseType(valueType, column.PropertyInvoker.TargetType))
                {
                    writer.WritePropertyName(column.Property);
                    if (column.DataType.IsEnum)
                    {
                        writer.WriteValue(column.PropertyInvoker.Get(item)?.ToString());
                    }
                    else
                    {
                        writer.WriteValue(column.PropertyInvoker.Get(item));
                    }
                }
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (!TypeHelper.IsBaseType(objectType, typeof(DBItem)))
            {
                throw new JsonSerializationException($"Expect {nameof(DBItem)} but {nameof(objectType)} is {objectType?.Name ?? "null"}");
            }
            var item = existingValue as DBItem;
            if (existingValue != null && item == null)
            {
                throw new JsonSerializationException($"Expect {nameof(DBItem)} but {nameof(existingValue)} is {existingValue?.GetType().Name ?? "null"}");
            }
            var table = DBTable.GetTable(objectType, null, false, true);
            if (table == null)
            {
                throw new JsonSerializationException($"Can't find table of {objectType?.Name ?? "null"}");
            }
            var column = (DBColumn)null;
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    column = table.ParseProperty((string)reader.Value) ?? table.ParseColumn((string)reader.Value);
                }
                else if (reader.TokenType == JsonToken.String
                    || reader.TokenType == JsonToken.Boolean
                    || reader.TokenType == JsonToken.Bytes
                    || reader.TokenType == JsonToken.Date
                    || reader.TokenType == JsonToken.Float
                    || reader.TokenType == JsonToken.Integer
                    || reader.TokenType == JsonToken.Float
                    || reader.TokenType == JsonToken.Null)
                {
                    if (column == null)
                        continue;
                    var value = column.ParseValue(reader.Value);
                    if (item == null)
                    {
                        if (column.IsPrimaryKey && value != null)
                        {
                            item = table.LoadItemById(value);
                        }
                        if (value == null && item == null)
                        {
                            item = table.NewItem();
                        }
                    }
                    else
                    {
                        if (column.ReferenceProperty != null)
                        {
                            value = column.ReferenceTable.LoadItemById(value);
                            column.ReferenceProperty.Set(item, value);
                        }
                        else
                        {
                            column.PropertyInvoker.Set(item, value);
                        }
                    }
                    column = null;
                }
            }
            return item;
        }


    }
}