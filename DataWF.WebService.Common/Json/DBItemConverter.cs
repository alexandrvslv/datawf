using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.WebService.Common
{
    public class DBItemConverter<T> : JsonConverter<T> where T : DBItem
    {
        public DBItemConverter()
        { }

        public DBItemConverter(DBItemConverterFactory factory)
        {
            Factory = factory;
            Table = DBService.GetTable<T>();
        }

        public DBTable<T> Table { get; set; }

        public DBItemConverterFactory Factory { get; set; }

        //public override bool CanConvert(Type objectType)
        //{
        //    return TypeHelper.IsBaseType(objectType, typeof(T));
        //}

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var settings = Factory.HttpJsonSettings;
            var valueType = value.GetType();
            var isRef = false;
            var columns = value.Table.GetTypeColumns(valueType);
            if (settings.Referenced && settings.Reference)
            {
                if (writer.CurrentDepth > 0 && Factory.referenceSet.Contains(value))
                {
                    isRef = true;
                    columns = value.Table.GetRefColumns();
                }
                else
                {
                    Factory.referenceSet.Add(value);
                }
            }
            writer.WriteStartObject();
            foreach (DBColumn column in columns)
            {
                if ((column.Keys & DBColumnKeys.Access) != 0)
                {
                    var propertyValue = ((IInvoker<DBItem, AccessValue>)column.PropertyInvoker).GetValue(value);
                    var accessValue = propertyValue.GetFlags(Factory.CurrentUser);
                    writer.WritePropertyName(column.JsonName);
                    JsonSerializer.Serialize<AccessType>(writer, accessValue, options);
                }
                else
                {
                    column.Write(writer, value, options);
                }
                if (column.IsReference)
                {
                    if (!settings.Referenced || writer.CurrentDepth > settings.MaxDepth)
                    {
                        continue;
                    }

                    var item = (DBItem)column.ReferencePropertyInvoker.GetValue(value);
                    if (!(item?.Access.GetFlag(AccessType.Read, Factory.CurrentUser) ?? true))
                    {
                        continue;
                    }
                    writer.WritePropertyName(column.JsonReferenceName);
                    JsonSerializer.Serialize(writer, item, column.ReferencePropertyInvoker.DataType, options);
                }
            }
            if (settings.Referencing && writer.CurrentDepth <= settings.MaxDepth && !isRef)
            {
                foreach (var referencing in value.Table.GetReferencing(valueType))
                {
                    writer.WritePropertyName(referencing.JsonName);

                    var typedEnumerable = referencing.PropertyInvoker.GetValue(value)?.ToEnumerable<DBItem>();
                    if (typedEnumerable != null)
                    {
                        if (typedEnumerable.FirstOrDefault() is DBGroupItem)
                        {
                            var buffer = typedEnumerable.Cast<DBGroupItem>().ToList();
                            ListHelper.QuickSort<DBGroupItem>(buffer, TreeComparer<DBGroupItem>.Default);
                            typedEnumerable = buffer;
                        }

                        writer.WriteStartArray();

                        foreach (var item in typedEnumerable)
                        {
                            if (!item.Access.GetFlag(AccessType.Read, Factory.CurrentUser))
                            {
                                continue;
                            }
                            JsonSerializer.Serialize(writer, item, item.GetType(), options);
                        }
                        writer.WriteEndArray();
                    }
                    else
                    {
                        writer.WriteNullValue();
                    }
                }
            }

            writer.WriteEndObject();
        }

        public override T Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
        {
            if (Table == null)
            {
                throw new JsonException($"Can't find table of {objectType?.Name ?? "null"}");
            }
            var item = Table.ItemTypeKey != null ? (T)null : (T)Table.NewItem();
            object id = null;
            int typeId = 0;
            var column = (DBColumn)null;
            var propertyName = (string)null;
            var dictionary = (Dictionary<IInvoker, object>)null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    propertyName = reader.GetString();
                    column = Table.ParseProperty(propertyName);
                }
                else
                {
                    if (column == null)
                    {
                        var invoker = Table.ParseReferencing(propertyName)?.PropertyInvoker;
                        if (invoker != null)
                        {
                            if (dictionary == null)
                                dictionary = new Dictionary<IInvoker, object>();
                            dictionary[invoker] = JsonSerializer.Deserialize(ref reader, invoker.DataType, options);
                        }
                        else
                        {
                            reader.Skip();
                            //throw new InvalidOperationException($"Property {propertyName} not found!");
                        }
                    }
                    else if (column.IsPrimaryKey)
                    {
                        if (item == null)
                        {
                            id = JsonSerializer.Deserialize(ref reader, column.DataType, options);
                        }
                        else
                        {
                            column.Read<T>(ref reader, item, options);
                        }
                    }
                    else if (column.IsTypeKey)
                    {
                        typeId = reader.GetInt32();
                        item = (T)Table.NewItem(DBUpdateState.Insert, true, typeId);
                    }
                    else if (item != null)
                    {
                        column.Read<T>(ref reader, item, options);
                    }
                    else
                    {
                        if (dictionary == null)
                            dictionary = new Dictionary<IInvoker, object>();
                        dictionary[column.PropertyInvoker] = JsonSerializer.Deserialize(ref reader, column.PropertyInvoker.DataType, options);
                    }
                }
            }

            item = (T)item.AttachOrUpdate(DBLoadParam.Load | DBLoadParam.Synchronize);

            if (dictionary != null)
            {
                foreach (var entry in dictionary)
                {
                    entry.Key.SetValue(item, entry.Value);
                }
            }
            return item;
        }


    }
}