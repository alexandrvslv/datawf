﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.Common
{

    public class JsonClientConverter<T, K> : JsonConverter<T>, IClientConverter
        where T : class, new()
        where K : struct
    {
        public JsonClientConverter()
        {
            SerializationInfo = new TypeSerializationInfo(typeof(T));
        }

        public JsonClientConverter(Client<T, K> client) : this()
        {
            Client = client;
        }

        ICrudClient IClientConverter.Client => Client;

        public Client<T, K> Client { get; set; }

        public TypeSerializationInfo SerializationInfo { get; }

        public override bool CanConvert(Type typeToConvert)
        {
            return TypeHelper.IsBaseType(typeToConvert, typeof(T));
        }

        public object Read(ref Utf8JsonReader jreader, object item, JsonSerializerOptions options)
        {
            return Read(ref jreader, item as T, options);
        }

        public override T Read(ref Utf8JsonReader jreader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Read(ref jreader, null, options);
        }

        public T Read(ref Utf8JsonReader jreader, T item, JsonSerializerOptions options)
        {
            var property = (PropertySerializationInfo)null;
            var id = (object)null;
            var synchItem = item as ISynchronized;
            while (jreader.Read() && jreader.TokenType != JsonTokenType.EndObject)
            {
                if (jreader.TokenType == JsonTokenType.PropertyName)
                {
                    property = SerializationInfo.GetProperty(jreader.GetString());
                }
                else
                {
                    if (property == null)
                    {
                        Deserialize(ref jreader, null, options, null, null);
                        continue;
                    }
                    var currentValue = item != null
                        && property.DataType != typeof(string)
                        && !property.DataType.IsValueType
                        ? property.Invoker.GetValue(item)
                        : null;

                    if (currentValue is IList listValue && synchItem != null && synchItem.SyncStatus == SynchronizedStatus.Load)
                    {
                        foreach (var listItem in listValue)
                        {
                            if (listItem is ISynchronized synchronized && synchronized.SyncStatus != SynchronizedStatus.Actual)
                            {
                                synchronized.SyncStatus = SynchronizedStatus.Load;
                            }
                        }
                    }
                    object value = Deserialize(ref jreader, property.DataType, options, currentValue, currentValue as IList);

                    if (property.Name == Client.TypeInvoker?.Name && value != null)
                    {
                        var typeId = (int)value;
                        if (typeId != Client.TypeId)
                        {
                            var client = Client.Provider.GetClient(typeof(T), typeId);
                            return (T)client.Converter.Read(ref jreader, item, options);
                        }
                        continue;
                    }
                    if (property.Name == Client.IdInvoker?.Name)
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

                        if (item is ISynchronized synchronized)
                        {
                            synchItem = synchronized;
                            if (synchItem.SyncStatus == SynchronizedStatus.Actual)
                            {
                                synchItem.SyncStatus = SynchronizedStatus.Load;
                            }
                        }

                        continue;
                    }
                    if (item == null)
                    {
                        throw new Exception("Wrong Json properties sequence!");
                    }
                    if (synchItem != null && synchItem.SyncStatus != SynchronizedStatus.Load
                        && synchItem.Changes.ContainsKey(property.Name))
                    {
                        continue;
                    }
                    property.Invoker.SetValue(item, value);
                }
            }
            if (item == null)
                return null;

            if (synchItem != null && synchItem.SyncStatus == SynchronizedStatus.Load)
            {
                synchItem.SyncStatus = SynchronizedStatus.Actual;
            }
            if (Client.RemoveDownloads((K)id))
            {
                Client.Add(item);
            }
            return item;
        }

        public virtual object DeserializeObject(ref Utf8JsonReader jreader, Type type, JsonSerializerOptions options, object item)
        {
            type = item?.GetType() ?? type;
            if (type == null)
            {
                while (jreader.Read() && jreader.TokenType != JsonTokenType.EndObject)
                { }
                return null;
            }
            //var client = Client.Provider.GetClient(type);
            //if (client != null)
            //    return (T)client.Converter.Read(ref jreader, item, options);
            //else
            return JsonSerializer.Deserialize(ref jreader, type, options);
        }

        public object Deserialize(ref Utf8JsonReader jreader, Type type, JsonSerializerOptions options, object item, IList sourceList)
        {
            object value = null;
            switch (jreader.TokenType)
            {
                case JsonTokenType.StartObject:
                    value = DeserializeObject(ref jreader, type, options, item);
                    break;
                case JsonTokenType.StartArray:
                    value = DeserializeArray(ref jreader, options, type, sourceList);
                    break;
                case JsonTokenType.String:
                //value = jreader.GetString();
                //value = Helper.TextParse((string)value, type, null);
                //break;
                case JsonTokenType.Number:
                    value = type == null ? null : JsonSerializer.Deserialize(ref jreader, type, options);
                    break;
                case JsonTokenType.True:
                    value = true;
                    break;
                case JsonTokenType.False:
                    value = false;
                    break;
                case JsonTokenType.Null:
                    value = null;
                    break;
            }
            return value;
        }

        protected virtual IList DeserializeArray(ref Utf8JsonReader jreader, JsonSerializerOptions options, Type type, IList sourceList)
        {
            if (type == null)
            {
                while (jreader.Read() && jreader.TokenType != JsonTokenType.EndArray)
                { }
                return null;
            }
            var itemType = TypeHelper.GetItemType(type);
            var client = Client.Provider.GetClient(itemType);
            var temp = sourceList ?? (IList)EmitInvoker.CreateObject(type);
            lock (temp)
            {
                var referenceList = temp as IReferenceList;
                if (referenceList != null
                    && referenceList.Owner.SyncStatus == SynchronizedStatus.Load)
                {
                    foreach (var item in referenceList.TypeOf<ISynchronized>())
                    {
                        item.SyncStatus = SynchronizedStatus.Load;
                    }
                }
                else
                {
                    temp.Clear();
                }
                while (jreader.Read() && jreader.TokenType != JsonTokenType.EndArray)
                {
                    var item = Deserialize(ref jreader, itemType, options, null, sourceList);
                    //client != null? client.Converter.Read(ref jreader, null, options): 
                    if (item == null)
                    {
                        continue;
                    }
                    temp.Add(item);
                }

                if (referenceList != null
                    && referenceList.Owner.SyncStatus == SynchronizedStatus.Load
                    && client != null)
                {
                    for (var i = 0; i < referenceList.Count; i++)
                    {
                        var item = referenceList[i];
                        if (item is ISynchronized synched
                            && synched.SyncStatus == SynchronizedStatus.Load)
                        {

                            if (!client.Remove(item))
                            {
                                referenceList.RemoveAt(i);
                            }
                            i--;
                        }
                    }
                }
            }
            return temp;
        }

        public override void Write(Utf8JsonWriter jwriter, T item, JsonSerializerOptions options)
        {
            var type = item.GetType();
            var typeInfo = SerializationInfo?.Type == type ? SerializationInfo : Serialization.Instance.GetTypeInfo(type);
            var synched = item as ISynchronized;

            jwriter.WriteStartObject();
            foreach (var property in typeInfo.Properties)
            {
                if (!property.IsWriteable
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
                    JsonSerializer.Serialize(jwriter, value, options);
                }
                else if (value is IList list)
                {
                    SerializeArray(jwriter, list, options);
                }
                else
                {
                    SerializeObject(jwriter, value, options);
                }
            }
            jwriter.WriteEndObject();
        }

        public void Serialize(Utf8JsonWriter jwriter, object item, JsonSerializerOptions options, TypeSerializationInfo info = null)
        {
            var type = item?.GetType();
            if (type == null || (info?.IsAttribute ?? TypeHelper.IsSerializeAttribute(type)))
            {
                JsonSerializer.Serialize(jwriter, item, options);
            }
            else if (item is IList list)
            {
                SerializeArray(jwriter, list, options);
            }
            else
            {
                SerializeObject(jwriter, item, options, info);
            }
        }

        public virtual void SerializeObject(Utf8JsonWriter jwriter, object item, JsonSerializerOptions options, TypeSerializationInfo info = null)
        {
            var type = item.GetType();
            var typeInfo = info?.Type == type ? info : Serialization.Instance.GetTypeInfo(type);
            var synched = item as ISynchronized;

            jwriter.WriteStartObject();
            foreach (var property in typeInfo.Properties)
            {
                if (!property.IsWriteable
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
                    JsonSerializer.Serialize(jwriter, value, options);
                }
                else if (value is IList list)
                {
                    SerializeArray(jwriter, list, options);
                }
                else
                {
                    SerializeObject(jwriter, value, options);
                }
            }
            jwriter.WriteEndObject();
        }

        protected virtual void SerializeArray(Utf8JsonWriter jwriter, IList list, JsonSerializerOptions options)
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
                Serialize(jwriter, item, options, itemInfo);
            }
            jwriter.WriteEndArray();
        }


    }
}