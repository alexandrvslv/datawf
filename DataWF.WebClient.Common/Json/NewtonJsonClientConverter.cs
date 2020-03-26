﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Common
{
    public class NewtonJsonClientConverter<T, K> : JsonConverter<T>, IClientConverter
        where T : class, new()
        where K : struct
    {
        public NewtonJsonClientConverter()
        {
            SerializationInfo = new TypeSerializationInfo(typeof(T));
        }

        public NewtonJsonClientConverter(Client<T, K> client) : this()
        {
            Client = client;
        }

        ICrudClient IClientConverter.Client => Client;

        public Client<T, K> Client { get; set; }

        public TypeSerializationInfo SerializationInfo { get; }

        public object Read(JsonReader jreader, object item, JsonSerializer serializer)
        {
            return Read(jreader, item as T, serializer);
        }

        public override T ReadJson(JsonReader jreader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return Read(jreader, existingValue, serializer);
        }

        public T Read(JsonReader jreader, T item, JsonSerializer serializer)
        {
            var property = (PropertySerializationInfo)null;
            var propertyType = (Type)null;
            var id = (object)null;
            var isRef = true;
            var synchItem = item as ISynchronized;
            while (jreader.Read() && jreader.TokenType != JsonToken.EndObject)
            {
                if (jreader.TokenType == JsonToken.PropertyName)
                {
                    property = SerializationInfo.GetProperty((string)jreader.Value);
                    propertyType = property?.DataType;
                }
                else
                {
                    if (property == null)
                    {
                        Deserialize(jreader, null, serializer, null);
                        continue;
                    }
                    var currentValue = item != null
                        && propertyType != typeof(string)
                        && !propertyType.IsValueType
                        ? property.Invoker.GetValue(item)
                        : null;

                    object value = Deserialize(jreader, property.DataType, serializer, currentValue);

                    if (string.Equals(property.Name, Client.TypeInvoker?.Name, StringComparison.Ordinal) && value != null)
                    {
                        var typeId = (int)value;
                        if (typeId != Client.TypeId)
                        {
                            var client = Client.Provider.GetClient(typeof(T), typeId);
#if NETSTANDARD2_0
                            return (T)client.Converter.Read(jreader, item, serializer);
#else
                            return (T)serializer.Deserialize(jreader, client.ItemType);
#endif
                        }
                        continue;
                    }
                    if (string.Equals(property.Name, Client.IdInvoker?.Name, StringComparison.Ordinal))
                    {
                        id = value;
                        if (item == null && id != null)
                        {
                            item = Client.Select((K)id) ?? Client.SelectBase(id);
                        }
                        if (item == null)
                        {
                            item = Client.AddDownloads((K)id, (p) => Client.NewLoadItem());
                        }
                        else if (!Client.Items.Contains(item))
                        {
                            item = Client.AddDownloads((K)id, item);
                        }
                        Client.IdInvoker.SetValue(item, id);
                        synchItem = item as ISynchronized;
                        continue;
                    }
                    if (item == null)
                    {
                        throw new Exception("Wrong Json properties sequence!");
                    }
                    if (isRef && synchItem != null)
                    {
                        isRef = false;

                        if (synchItem.SyncStatus == SynchronizedStatus.Actual
                            || synchItem.SyncStatus == SynchronizedStatus.Suspend)
                        {
                            synchItem.SyncStatus = SynchronizedStatus.Load;
                        }
                    }
                    if (synchItem != null
                        && synchItem.SyncStatus != SynchronizedStatus.Load
                        && synchItem.Changes.ContainsKey(property.Name))
                    {
                        continue;
                    }
                    property.Invoker.SetValue(item, value);
                }
            }
            if (item == null)
                return null;

            if (synchItem != null)
            {
                if ((!isRef && synchItem.SyncStatus == SynchronizedStatus.Load)
                    || synchItem.SyncStatus == SynchronizedStatus.Suspend)
                    synchItem.SyncStatus = SynchronizedStatus.Actual;
            }

            if (Client.RemoveDownloads((K)id))
            {
                Client.Add(item);
            }
            return item;
        }

        public virtual object DeserializeObject(JsonReader jreader, Type type, JsonSerializer serializer, object item)
        {
            type = item?.GetType() ?? type;
            if (type == null)
            {
                while (jreader.Read() && jreader.TokenType != JsonToken.EndObject)
                { }
                return null;
            }
            //var client = Client.Provider.GetClient(type);
            //if (client != null)
            //    return (T)client.Converter.Read(ref jreader, item, options);
            //else
            return serializer.Deserialize(jreader, type);
        }

        public object Deserialize(JsonReader jreader, Type type, JsonSerializer serializer, object item)
        {
            object value = null;
            switch (jreader.TokenType)
            {
                case JsonToken.StartObject:
                    value = DeserializeObject(jreader, type, serializer, item);
                    break;
                case JsonToken.StartArray:
                    value = DeserializeArray(jreader, serializer, type, item as IList);
                    break;
                case JsonToken.String:
                case JsonToken.Date:
                case JsonToken.Float:
                case JsonToken.Bytes:
                case JsonToken.Raw:
                case JsonToken.Boolean:
                //value = jreader.GetString();
                //value = Helper.TextParse((string)value, type, null);
                //break;
                case JsonToken.Integer:
                    value = type == null ? null : serializer.Deserialize(jreader, type);
                    break;
                case JsonToken.Null:
                    value = null;
                    break;
            }
            return value;
        }

        protected virtual IList DeserializeArray(JsonReader jreader, JsonSerializer serializer, Type type, IList sourceList)
        {
            if (type == null)
            {
                while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
                { }
                return null;
            }
            var itemType = TypeHelper.GetItemType(type);
            var client = Client.Provider.GetClient(itemType);
            var temp = sourceList ?? (IList)EmitInvoker.CreateObject(type);
            var referenceList = temp as IReferenceList;
            if (referenceList != null && client != null
                && referenceList.Owner.SyncStatus == SynchronizedStatus.Load)
            {
                var referanceBuffer = new HashSet<ISynchronized>((IEnumerable<ISynchronized>)referenceList);
                while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
                {
#if NETSTANDARD2_0
                    var item = client.Converter.Read(jreader, null, serializer);
#else
                    var item = Deserialize(jreader, itemType, serializer, null);
#endif
                    if (item is ISynchronized synched)
                    {
                        referenceList.Add(item);
                        referanceBuffer.Remove(synched);
                    }

                }
                foreach (var item in referanceBuffer)
                {
                    if (!client.Remove(item))
                    {
                        referenceList.Remove(item);
                    }
                }
            }
            else
            {
                temp.Clear();
                while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
                {
                    var item = Deserialize(jreader, itemType, serializer, null);
                    if (item == null)
                    {
                        continue;
                    }
                    temp.Add(item);
                }
            }

            return temp;
        }

        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            Write(writer, value, serializer);
        }

        public void Write(JsonWriter jwriter, T item, JsonSerializer serializer)
        {
            var type = item.GetType();
            var typeInfo = SerializationInfo?.Type == type ? SerializationInfo : Serialization.Instance.GetTypeInfo(type);
            var synched = item as ISynchronized;

            jwriter.WriteStartObject();
            foreach (var property in typeInfo.Properties)
            {
                if (!property.IsWriteable
                    || property.IsReadOnly
                    || (property.IsChangeSensitive
                    && synched != null
                    && !(synched.Changes.ContainsKey(property.Name))))
                {
                    continue;
                }

                jwriter.WritePropertyName(property.Name);
                var value = property.Invoker.GetValue(item);
                if (property.IsAttribute || value == null)
                {
                    serializer.Serialize(jwriter, value, property.DataType);
                }
                else if (value is IList list)
                {
                    SerializeArray(jwriter, list, serializer);
                }
                else
                {
                    serializer.Serialize(jwriter, value, property.DataType);
                }
            }
            jwriter.WriteEndObject();
        }

        public void Serialize(JsonWriter jwriter, object item, JsonSerializer serializer, TypeSerializationInfo info = null)
        {
            var type = item?.GetType();
            if (type == null || (info?.IsAttribute ?? TypeHelper.IsSerializeAttribute(type)))
            {
                serializer.Serialize(jwriter, item);
            }
            else if (item is IList list)
            {
                SerializeArray(jwriter, list, serializer);
            }
            else
            {
                serializer.Serialize(jwriter, item, type);
            }
        }

        protected virtual void SerializeArray(JsonWriter jwriter, IList list, JsonSerializer serializer)
        {
            jwriter.WriteStartArray();
            var itemType = TypeHelper.GetItemType(list);
            var itemInfo = Serialization.Instance.GetTypeInfo(itemType);
            foreach (var item in list)
            {
                if (item is ISynchronized isSynch && isSynch.SyncStatus == SynchronizedStatus.Actual)
                {
                    continue;
                }
                Serialize(jwriter, item, serializer, itemInfo);
            }
            jwriter.WriteEndArray();
        }
#if !NETSTANDARD2_0
        object IClientConverter.Read(ref System.Text.Json.Utf8JsonReader jreader, object item, System.Text.Json.JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }
#endif
    }
}