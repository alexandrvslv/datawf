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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Web.Common
{

    public class DBItemJsonConverter : JsonConverter
    {
        public bool IsSerializeableColumn(DBColumn column)
        {
            return column.PropertyInvoker != null
                //&& (column.Attribute.Keys & DBColumnKeys.Access) != DBColumnKeys.Access
                && (column.Keys & DBColumnKeys.Password) != DBColumnKeys.Password
                && (column.Keys & DBColumnKeys.File) != DBColumnKeys.File;
        }

        public override bool CanConvert(Type objectType)
        {
            return TypeHelper.IsBaseType(objectType, typeof(DBItem));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is DBItem item)
            {
                var claimsWriter = writer as ClaimsJsonTextWriter;
                writer.WriteStartObject();
                var table = item.Table;
                var valueType = value.GetType();
                var includeReference = claimsWriter?.IncludeReferences ?? false;

                foreach (var column in table.Columns.Where(p => TypeHelper.IsBaseType(valueType, p.PropertyInvoker.TargetType)))
                {
                    if (!IsSerializeableColumn(column))
                        continue;
                    writer.WritePropertyName(column.Property);
                    var propertyValue = column.PropertyInvoker.GetValue(item);
                    if (propertyValue is AccessValue accessValue)
                    {
                        serializer.Serialize(writer, accessValue.GetFlags(claimsWriter.User));
                    }
                    else
                    {
                        serializer.Serialize(writer, propertyValue);
                        if (includeReference && column.ReferencePropertyInvoker != null)
                        {
                            writer.WritePropertyName(column.ReferencePropertyInfo.Name);
                            serializer.Serialize(writer, column.ReferencePropertyInfo.GetValue(item));
                        }
                    }
                }

                if (claimsWriter?.IncludeReferencing ?? true && table.TableAttribute != null)
                {
                    foreach (var refing in table.TableAttribute.Referencings)
                    {
                        if (!TypeHelper.IsBaseType(valueType, refing.PropertyInvoker.TargetType))
                            continue;
                        if (refing.PropertyInvoker.GetValue(item) is IEnumerable<DBItem> refs)
                        {
                            writer.WritePropertyName(refing.PropertyInfo.Name);
                            serializer.Serialize(writer, refs);
                        }
                    }
                }
                writer.WriteEndObject();
            }
            else
            {
                serializer.Serialize(writer, value);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var item = existingValue as DBItem;
            if (existingValue != null && item == null)
            {
                throw new JsonSerializationException($"Expect {nameof(DBItem)} but {nameof(existingValue)} is {existingValue?.GetType().Name ?? "null"}");
            }
            var table = DBTable.GetTable(objectType);
            if (table == null)
            {
                throw new JsonSerializationException($"Can't find table of {objectType?.Name ?? "null"}");
            }
            var dictionary = new Dictionary<IInvoker, object>();
            var key = (IInvoker)null;
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    key = table.GetInvoker((string)reader.Value);
                    if (key == null)
                    {
                        throw new InvalidOperationException($"Property {reader.Value} not found!");
                    }
                }
                else if (key != null)
                {
                    dictionary[key] = serializer.Deserialize(reader, key.DataType);
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