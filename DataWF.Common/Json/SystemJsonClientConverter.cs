using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.Common
{
    public class SystemJsonClientConverter<T, K> : JsonConverter<T>, IWebTableConverter
        where T : class, new()
        where K : struct
    {
        public SystemJsonClientConverter()
        {
            SerializationInfo = Serialization.Instance.GetTypeInfo(typeof(T));
        }

        public SystemJsonClientConverter(WebTable<T, K> table) : this()
        {
            Table = table;
        }

        IWebTable IWebTableConverter.Table => Table;

        public WebTable<T, K> Table { get; set; }

        public TypeSerializeInfo SerializationInfo { get; }

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
            if (jreader.TokenType == JsonTokenType.Null)
            {
                return default(T);
            }
            return Read(ref jreader, null, options);
        }

        public T Read(ref Utf8JsonReader jreader, T item, JsonSerializerOptions options)
        {
            var property = (IPropertySerializeInfo)null;
            var propertyType = (Type)null;
            var id = (K?)null;
            var isRef = true;
            var synchItem = item as ISynchronized;
            while (jreader.Read() && jreader.TokenType != JsonTokenType.EndObject)
            {
                if (jreader.TokenType == JsonTokenType.PropertyName)
                {
                    property = SerializationInfo.GetProperty(jreader.GetString());
                    propertyType = property?.DataType;
                }
                else
                {
                    if (property == null)
                    {
                        Read(ref jreader, null, options, null);
                        continue;
                    }
                    var currentValue = item != null
                        && propertyType != typeof(string)
                        && !propertyType.IsValueType
                        ? property.PropertyInvoker.GetValue(item)
                        : null;

                    if (string.Equals(property.Name, Table.TypeInvoker?.Name, StringComparison.Ordinal))
                    {
                        var typeId = JsonSerializer.Deserialize<int>(ref jreader, options);
                        if (typeId != Table.TypeId)
                        {
                            var table = Table.Schema.GetTable(typeof(T), typeId);
#if NETSTANDARD2_0
                            return (T)JsonSerializer.Deserialize(ref jreader, table.ItemType, options);
#else
                            return (T)table.Converter.Read(ref jreader, item, options);
#endif
                        }
                        continue;
                    }
                    if (string.Equals(property.Name, Table.IdInvoker?.Name, StringComparison.Ordinal))
                    {
                        id = JsonSerializer.Deserialize<K>(ref jreader, options);
                        if (item == null && id != null)
                        {
                            item = Table.Select((K)id);
                            if (item == null)
                            {
                                item = Table.AddDownloads((K)id, Table.NewLoadItem);
                            }
                        }
                        else if (id == null)
                        {
                            item = (T)Table.NewItem();
                        }
                        else if (!Table.Items.Contains(item))
                        {
                            item = Table.AddDownloads((K)id, item);
                        }
                        synchItem = item as ISynchronized;
                        continue;
                    }
                    if (item == null)
                    {
                        throw new Exception("Wrong Json properties sequence!");
                    }

                    isRef = false;
                    lock (item)
                    {
                        if (synchItem.SyncStatus == SynchronizedStatus.Actual)
                        {
                            synchItem.SyncStatus = SynchronizedStatus.Load;
                        }
                        else if (synchItem.SyncStatus != SynchronizedStatus.Load
                            && synchItem.Changes.ContainsKey(property.Name))
                        {
                            JsonSerializer.Deserialize(ref jreader, property.DataType, options);
                            continue;
                        }
                        if (property.IsAttribute)
                        {
                            property.Read(ref jreader, item);
                        }
                        else
                        {
                            object value = Read(ref jreader, property.DataType, options, currentValue);
                            property.PropertyInvoker.SetValue(item, value);
                        }
                    }
                }
            }
            if (item == null)
                return null;

            if (!isRef)
            {
                lock (synchItem)
                {
                    if ((synchItem.SyncStatus == SynchronizedStatus.Load))
                    {
                        synchItem.SyncStatus = SynchronizedStatus.Actual;
                    }

                    if (id != null && Table.RemoveDownloads((K)id))
                    {
                        Table.Add(item);
                    }
                }
            }
            return item;
        }

        public object Read(ref Utf8JsonReader jreader, Type type, JsonSerializerOptions options, object item)
        {
            object value = null;
            switch (jreader.TokenType)
            {
                case JsonTokenType.StartObject:
                    value = ReadObject(ref jreader, type, options, item);
                    break;
                case JsonTokenType.StartArray:
                    value = ReadArray(ref jreader, options, type, item as IList);
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

        public virtual object ReadObject(ref Utf8JsonReader jreader, Type type, JsonSerializerOptions options, object item)
        {
            type = item?.GetType() ?? type;
            if (type == null)
            {
                while (jreader.Read() && jreader.TokenType != JsonTokenType.EndObject)
                { }
                return null;
            }
            return JsonSerializer.Deserialize(ref jreader, type, options);
        }

        protected virtual IList ReadArray(ref Utf8JsonReader jreader, JsonSerializerOptions options, Type type, IList sourceList)
        {
            if (type == null)
            {
                while (jreader.Read() && jreader.TokenType != JsonTokenType.EndArray)
                { }
                return null;
            }
            var typeInfo = Serialization.Instance.GetTypeInfo(type);
            var itemType = typeInfo.ListItemType;
            var client = Table.Schema.GetTable(itemType);
            var temp = sourceList ?? (IList)EmitInvoker.CreateObject(type);
            var referenceList = temp as IReferenceList;
            if (referenceList != null && client != null)
            {
                var isLoad = referenceList.Owner.SyncStatus == SynchronizedStatus.Load
                    || referenceList.Owner.SyncStatus == SynchronizedStatus.Actual;
                if (isLoad)
                {
                    foreach (ISynchronized item in referenceList)
                    {
                        item.SyncStatus = SynchronizedStatus.Actual;
                    }
                }                
                while (jreader.Read() && jreader.TokenType != JsonTokenType.EndArray)
                {
#if NETSTANDARD2_0
                    var item = Read(ref jreader, itemType, options, null);
#else
                    var item = client.Converter.Read(ref jreader, itemType, options);
#endif
                    if (item is ISynchronized synched)
                    {
                        referenceList.Add(item);
                    }

                }
                //foreach (var item in referanceBuffer)
                //{
                //    if (!client.Remove(item))
                //    {
                //        referenceList.Remove(item);
                //    }
                //}
            }
            else
            {
                temp.Clear();
                while (jreader.Read() && jreader.TokenType != JsonTokenType.EndArray)
                {
                    var item = Read(ref jreader, itemType, options, null);
                    if (item == null)
                    {
                        continue;
                    }
                    temp.Add(item);
                }
            }
            return temp;
        }

        public override void Write(Utf8JsonWriter jwriter, T item, JsonSerializerOptions options)
        {
            var type = item.GetType();
            var typeInfo = SerializationInfo?.Type == type ? SerializationInfo : Serialization.Instance.GetTypeInfo(type);
            var synched = item as ISynchronized;
            SystemJsonConverterFactory.WriterContexts.TryGetValue(jwriter, out var context);
            context?.Items.Add(item);
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
                if (property.IsAttribute)
                {
                    property.Write(jwriter, item);
                }
                else
                {
                    var value = property.PropertyInvoker.GetValue(item);
                    if (value is ISynchronized synchedValue)
                    {
                        if (context != null && context.Items.Contains(value))
                            continue;

                        //if (synchedValue.SyncStatus != SynchronizedStatus.New
                        //    && synchedValue.SyncStatus != SynchronizedStatus.Edit)
                        {
                            continue;
                        }
                    }

                    if (value == null)
                    {
                        jwriter.WriteNull(property.JsonName);
                    }
                    else if (value is IList list)
                    {
                        jwriter.WritePropertyName(property.JsonName);
                        WriteList(jwriter, list, options);
                    }
                    else
                    {
                        jwriter.WritePropertyName(property.JsonName);
                        JsonSerializer.Serialize(jwriter, value, property.DataType, options);
                    }
                }
            }
            jwriter.WriteEndObject();
        }

        public void Write(Utf8JsonWriter jwriter, object item, JsonSerializerOptions options, TypeSerializeInfo info = null)
        {
            var type = item?.GetType();
            if (type == null || (info?.IsAttribute ?? TypeHelper.IsSerializeAttribute(type)))
            {
                JsonSerializer.Serialize(jwriter, item, options);
            }
            else if (item is IList list)
            {
                WriteList(jwriter, list, options);
            }
            else
            {
                JsonSerializer.Serialize(jwriter, item, options);
            }
        }

        protected virtual void WriteList(Utf8JsonWriter jwriter, IList list, JsonSerializerOptions options)
        {
            jwriter.WriteStartArray();
            var listInfo = Serialization.Instance.GetTypeInfo(list.GetType());
            var itemType = listInfo.ListItemType;
            var itemInfo = Serialization.Instance.GetTypeInfo(itemType);
            SystemJsonConverterFactory.WriterContexts.TryGetValue(jwriter, out var context);
            foreach (var item in list)
            {
                if (item is ISynchronized isSynch
                    && (isSynch.SyncStatus == SynchronizedStatus.Actual
                    || (context?.Items.Contains(item) ?? false)))
                {
                    //TODO serialize reference (TypeId, PrimaryId)
                    continue;
                }
                Write(jwriter, item, options, itemInfo);
            }
            jwriter.WriteEndArray();
        }
#if NETSTANDARD2_0
        object IWebTableConverter.Read(Newtonsoft.Json.JsonReader jreader, object item, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
#endif
    }
}