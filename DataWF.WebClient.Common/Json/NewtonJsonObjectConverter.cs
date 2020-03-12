using Newtonsoft.Json;
using System;
using System.Collections;

namespace DataWF.Common
{
    public class NewtonJsonObjectConverter<T> : JsonConverter<T> where T : class, new()
    {
        public NewtonJsonObjectConverter()
        {
            SerializationInfo = new TypeSerializationInfo(typeof(T));
        }

        public TypeSerializationInfo SerializationInfo { get; }
        public override T ReadJson(JsonReader jreader, Type objectType, T item, bool hasExistingValue, JsonSerializer serializer)
        {
            PropertySerializationInfo property = null;
            item = item ?? new T();
            var synchItem = item as ISynchronized;
            while (jreader.Read() && jreader.TokenType != JsonToken.EndObject)
            {
                if (jreader.TokenType == JsonToken.PropertyName)
                {
                    property = SerializationInfo.GetProperty((string)jreader.Value);
                }
                else
                {
                    if (property == null)
                    {
                        Read(jreader, null, serializer, null, null);
                        continue;
                    }
                    var currentValue = item != null
                        && property.DataType != typeof(string)
                        && !property.DataType.IsValueType
                        ? property.Invoker.GetValue(item)
                        : null;

                    object value = Read(jreader, property.DataType, serializer, currentValue, currentValue as IList);

                    if (synchItem != null && synchItem.SyncStatus != SynchronizedStatus.Load
                        && synchItem.Changes.ContainsKey(property.Name))
                    {
                        continue;
                    }
                    property.Invoker.SetValue(item, value);
                }
            }
            if (synchItem != null && synchItem.SyncStatus == SynchronizedStatus.Load)
            {
                synchItem.SyncStatus = SynchronizedStatus.Actual;
            }
            return item;
        }

        public virtual object ReadObject(JsonReader jreader, Type type, JsonSerializer serializer, object item)
        {
            type = item?.GetType() ?? type;
            if (type == null)
            {
                while (jreader.Read() && jreader.TokenType != JsonToken.EndObject)
                { }
                return null;
            }
            return serializer.Deserialize(jreader, type);
        }

        public object Read(JsonReader jreader, Type type, JsonSerializer serializer, object item, IList sourceList)
        {
            object value = null;
            switch (jreader.TokenType)
            {
                case JsonToken.StartObject:
                    value = ReadObject(jreader, type, serializer, item);
                    break;
                case JsonToken.StartArray:
                    value = ReadArray(jreader, serializer, type, sourceList);
                    break;
                case JsonToken.String:
                //value = jreader.GetString();
                //value = Helper.TextParse((string)value, type, null);
                //break;
                case JsonToken.Date:
                case JsonToken.Float:
                case JsonToken.Bytes:
                case JsonToken.Raw:
                case JsonToken.Integer:
                    value = type == null ? null : serializer.Deserialize(jreader, type);
                    break;
                case JsonToken.Null:
                    value = null;
                    break;
            }
            return value;
        }

        protected virtual IList ReadArray(JsonReader jreader, JsonSerializer serializer, Type type, IList sourceList)
        {
            if (type == null)
            {
                while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
                { }
                return null;
            }
            var itemType = TypeHelper.GetItemType(type);
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
                while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
                {
                    var item = Read(jreader, itemType, serializer, null, sourceList);
                    if (item == null)
                    {
                        continue;
                    }
                    temp.Add(item);
                }

                if (referenceList != null
                    && referenceList.Owner.SyncStatus == SynchronizedStatus.Load)
                {
                    for (var i = 0; i < referenceList.Count; i++)
                    {
                        var item = referenceList[i];
                        if (item is ISynchronized synched
                            && synched.SyncStatus == SynchronizedStatus.Load)
                        {

                            referenceList.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
            return temp;
        }

        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}