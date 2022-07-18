using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.WebService.Common
{
    public class DBItemFullConverter<T> : JsonConverter<T> where T : DBItem, new()
    {
        public DBItemFullConverter()
        {
        }

        public DBItemFullConverter(DBItemConverterFactory factory)
        {
            Factory = factory;
        }

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
            var invokers = value.Table.GetInvokers(valueType);

            if (settings.Referenced && settings.Reference)
            {
                Factory.referenceSet.Add(value);
            }

            writer.WriteStartObject();
            foreach (IInvokerJson invoker in invokers)
            {
                var propertyType = invoker.DataType;
                if (TypeHelper.IsBaseType(propertyType, typeof(DBItem)))
                {
                    if (!settings.Referenced || writer.CurrentDepth > settings.MaxDepth)
                    {
                        continue;
                    }

                    var item = (DBItem)invoker.GetValue(value);
                    if (!(item?.Access.GetFlag(AccessType.Read, Factory.CurrentUser) ?? true))
                    {
                        continue;
                    }
                    writer.WritePropertyName(invoker.JsonName);
                    JsonSerializer.Serialize(writer, item, propertyType, options);
                }
                else if (propertyType == typeof(AccessValue))
                {
                    var propertyValue = (AccessValue)invoker.GetValue(value);
                    var accessValue = propertyValue.GetFlags(Factory.CurrentUser);
                    writer.WritePropertyName(invoker.JsonName);
                    JsonSerializer.Serialize<AccessType>(writer, accessValue, options);
                }
                else
                {
                    writer.WritePropertyName(invoker.JsonName);
                    invoker.WriteValue(writer, value, options);
                }
            }
            if (settings.Referencing && writer.CurrentDepth <= settings.MaxDepth && !isRef)
            {
                foreach (IInvokerJson invoker in value.Table.GetRefingInvokers(valueType))
                {
                    writer.WritePropertyName(invoker.JsonName);

                    var typedEnumerable = invoker.GetValue(value)?.ToEnumerable<DBItem>();
                    if (typedEnumerable != null)
                    {
                        if (typedEnumerable.FirstOrDefault() is DBGroupItem)
                        {
                            var buffer = typedEnumerable.ToList();
                            ListHelper.QuickSort(buffer, TreeComparer<IGroup>.Default);
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

        //public override bool CanConvert(Type objectType)
        //{
        //    return TypeHelper.IsBaseType(objectType, typeof(T));
        //}

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var settings = Factory.HttpJsonSettings;
            var valueType = value.GetType();
            var isRef = false;
            var invokers = value.Table.GetInvokers(valueType);
            if (settings.Referenced && settings.Reference)
            {
                if (writer.CurrentDepth > 0 && Factory.referenceSet.Contains(value))
                {
                    isRef = true;
                    invokers = value.Table.GetRefInvokers();
                }
                else
                {
                    Factory.referenceSet.Add(value);
                }
            }

            writer.WriteStartObject();
            foreach (IInvokerJson invoker in invokers)
            {
                var propertyType = invoker.DataType;
                if (TypeHelper.IsBaseType(propertyType, typeof(DBItem)))
                {
                    if (!settings.Referenced || writer.CurrentDepth > settings.MaxDepth)
                    {
                        continue;
                    }

                    var item = (DBItem)invoker.GetValue(value);
                    if (!(item?.Access.GetFlag(AccessType.Read, Factory.CurrentUser) ?? true))
                    {
                        continue;
                    }
                    writer.WritePropertyName(invoker.JsonName);
                    JsonSerializer.Serialize(writer, item, propertyType, options);
                }
                else if (propertyType == typeof(AccessValue))
                {
                    var propertyValue = (AccessValue)invoker.GetValue(value);
                    var accessValue = propertyValue.GetFlags(Factory.CurrentUser);
                    writer.WritePropertyName(invoker.JsonName);
                    JsonSerializer.Serialize<AccessType>(writer, accessValue, options);
                }
                else
                {
                    writer.WritePropertyName(invoker.JsonName);
                    invoker.WriteValue(writer, value, options);
                }
            }
            if (settings.Referencing && writer.CurrentDepth <= settings.MaxDepth && !isRef)
            {
                foreach (IInvokerJson invoker in value.Table.GetRefingInvokers(valueType))
                {
                    writer.WritePropertyName(invoker.JsonName);

                    var typedEnumerable = invoker.GetValue(value)?.ToEnumerable<DBItem>();
                    if (typedEnumerable != null)
                    {
                        if (typedEnumerable.FirstOrDefault() is DBGroupItem)
                        {
                            var buffer = typedEnumerable.ToList();
                            ListHelper.QuickSort(buffer, TreeComparer<IGroup>.Default);
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
                    if (TypeHelper.IsBaseType(invoker.DataType, typeof(DBItem)))
                    {
                        reader.Skip();
                        continue;
                    }
                    //if (propertyName == null || propertyName.Equals("access"))
                    //    continue;
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
            if(dictionary.Count > 0)
            {
                try
                {
                    foreach (var entry in dictionary)
                    {
                        if(entry.Value == null)
                            continue;
                        entry.Key.SetValue(item, entry.Value);
                    }
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            }
            return item;
        }


    }
}
