using DataWF.Collections;
using DataWF.Invokers;
using DataWF.Models;
using DataWF.Serialization;
using DataWF.Serialization.Elements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF
{
    public class SystemJsonClientConverter<T, K> : TypeInfoSerializer<T>, IModelTableConverter
        where T : class, new()
        where K : struct
    {
        public SystemJsonClientConverter()
        {
            SerializationInfo = SerializeProvider.GetTypeInfo<T>();
        }

        public SystemJsonClientConverter(WebTable<T, K> table) : this()
        {
            Table = table;
        }

        public WebTable<T, K> Table { get; set; }

        public ISerializeTypeInfo<T> SerializationInfo { get; }

        IModelTable IModelTableConverter.Table => Table;

        ISerializeTypeInfo IModelTableConverter.SerializationInfo => SerializationInfo;

        public override bool CanConvert(Type typeToConvert)
        {
            return TypeHelper.IsBaseType(typeToConvert, typeof(T));
        }

        public override void Read(ref Utf8JsonReader jreader, scoped ref T item, JsonSerializerOptions options)
        {
            if (jreader.TokenType == JsonTokenType.Null)
            {
                item = default(T);
                return;
            }
            var property = (ISerializePropertyInfo<T>?)null;
            var propertyType = (Type?)null;
            var id = default(K);
            var isRef = true;
            var typeId = 0;

            while (jreader.Read() && jreader.TokenType != JsonTokenType.EndObject)
            {
                if (jreader.TokenType == JsonTokenType.PropertyName)
                {
                    property = SerializationInfo.GetProperty(jreader.GetString());
                    propertyType = property?.Invoker.DataType;
                }
                else
                {
                    if (property == null)
                    {
                        jreader.Skip();
                        continue;
                    }
                    var currentValue = item != null
                        && propertyType != typeof(string)
                        && !propertyType.IsValueType
                        ? property.Invoker.GetValue(item)
                        : null;

                    if (string.Equals(property.Name, Table.TypeInvoker?.Name, StringComparison.Ordinal))
                    {
                        ((ElementSerializer<int>)property.Serializer).Read(ref jreader, ref typeId, options);
                        if (typeId != Table.TypeId)
                        {
                            var table = Table.Schema.GetTable(typeof(T), typeId);
                            table.Converter.Read(ref jreader, item, options);
                            return;
                        }
                        continue;
                    }
                    if (string.Equals(property.Name, Table.IdInvoker?.Name, StringComparison.Ordinal))
                    {
                        ((ElementSerializer<K>)property.Serializer).Read(ref jreader, ref id, options);
                        if (item == null && !EqualityComparer<K>.Default.Equals(id, default(K)))
                        {
                            item = Table.SelectNoDownloads(id);
                            if (item == null)
                            {
                                item = Table.Loader.AddDownloads(id, Table.NewLoadItem);
                            }
                        }
                        else if (EqualityComparer<K>.Default.Equals(id, default(K)))
                        {
                            item = (T)Table.NewItem();
                        }
                        else if (!Table.Contains(item))
                        {
                            item = Table.Loader.AddDownloads(id, item);
                        }
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
                        property.Read(ref jreader, ref item, options);
                    }
                }
            }
            if (item == null)
                return;

            if (!isRef)
            {
                lock (item)
                {
                    if ((item.SyncStatus == SynchronizedStatus.Load))
                    {
                        item.SyncStatus = SynchronizedStatus.Actual;
                    }

                    if (id != null && Table.RemoveDownloads((K)id))
                    {
                        Table.Add(item);
                    }
                }
            }
        }

        protected virtual IList ReadArray(ref Utf8JsonReader jreader, JsonSerializerOptions options, Type type, IList sourceList)
        {
            if (type == null)
            {
                while (jreader.Read() && jreader.TokenType != JsonTokenType.EndArray)
                { }
                return null;
            }
            var typeInfo = SerializeProvider.GetTypeInfo(type);
            var itemType = typeInfo.ListItemType;
            var client = Table.Schema.GetTable(itemType);
            var temp = sourceList ?? (IList)InvokeProvider.CreateObject(type);
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
            var typeInfo = SerializationInfo?.Type == type ? SerializationInfo : SerializeProvider.GetTypeInfo(type);
            var synched = item as ISynchronized;
            SystemJsonConverterFactory.WriterContexts.TryGetValue(jwriter, out var context);
            context?.Items.Add(item);
            jwriter.WriteStartObject();
            foreach (var property in typeInfo.Properties)
            {
                if (!property.IsSerializeable
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
                    var value = property.Invoker.GetValue(item);
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

        public void Write(Utf8JsonWriter jwriter, object item, JsonSerializerOptions options, ISerializeTypeInfo info = null)
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
            var listInfo = SerializeProvider.GetTypeInfo(list.GetType());
            var itemType = listInfo.ListItemType;
            var itemInfo = SerializeProvider.GetTypeInfo(itemType);
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
    }
}