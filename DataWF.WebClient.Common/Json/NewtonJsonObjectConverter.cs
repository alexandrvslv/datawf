using Newtonsoft.Json;
using System;
using System.Collections;

namespace DataWF.Common
{
    public class NewtonJsonObjectConverter<T> : JsonConverter<T> where T : class, new()
    {
        public NewtonJsonObjectConverter()
        {
            SerializationInfo = Serialization.Instance.GetTypeInfo(typeof(T));
        }

        public TypeSerializationInfo SerializationInfo { get; }

        public override T ReadJson(JsonReader jreader, Type objectType, T item, bool hasExistingValue, JsonSerializer serializer)
        {
            if (jreader.TokenType == JsonToken.Null)
            {
                return default(T);
            }
            var property = (PropertySerializationInfo)null;
            var propertyType = (Type)null;
            if (item == null)
                item = new T();
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
                        Read(jreader, null, serializer, null);
                        continue;
                    }
                    var currentValue = item != null
                        && propertyType != typeof(string)
                        && !propertyType.IsValueType
                        ? property.Invoker.GetValue(item)
                        : null;

                    object value = Read(jreader, property.DataType, serializer, currentValue);

                    property.Invoker.SetValue(item, value);
                }
            }
            if (item == null)
                return null;

            return item;
        }

        public object Read(JsonReader jreader, Type type, JsonSerializer serializer, object item)
        {
            object value = null;
            switch (jreader.TokenType)
            {
                case JsonToken.StartObject:
                    value = ReadObject(jreader, type, serializer, item);
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

        protected virtual IList DeserializeArray(JsonReader jreader, JsonSerializer serializer, Type type, IList sourceList)
        {
            if (type == null)
            {
                while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
                { }
                return null;
            }
            var typeInfo = Serialization.Instance.GetTypeInfo(type);
            var itemType = typeInfo.ListItemType;

            var temp = sourceList ?? (IList)EmitInvoker.CreateObject(type);
            temp.Clear();
            while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
            {
                var item = Read(jreader, itemType, serializer, null);
                if (item == null)
                {
                    continue;
                }
                temp.Add(item);
            }
            return temp;
        }

        public override void WriteJson(JsonWriter jwriter, T item, JsonSerializer serializer)
        {
            var type = item.GetType();
            var typeInfo = SerializationInfo?.Type == type ? SerializationInfo : Serialization.Instance.GetTypeInfo(type);

            jwriter.WriteStartObject();
            foreach (var property in typeInfo.Properties)
            {
                if (!property.IsWriteable
                    || property.IsReadOnly)
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
                    WriteArray(jwriter, list, serializer);
                }
                else
                {
                    serializer.Serialize(jwriter, value, property.DataType);
                }
            }
            jwriter.WriteEndObject();
        }

        public void Write(JsonWriter jwriter, object item, JsonSerializer serializer, TypeSerializationInfo info = null)
        {
            var type = item?.GetType();
            if (type == null || (info?.IsAttribute ?? TypeHelper.IsSerializeAttribute(type)))
            {
                serializer.Serialize(jwriter, item);
            }
            else if (item is IList list)
            {
                WriteArray(jwriter, list, serializer);
            }
            else
            {
                serializer.Serialize(jwriter, item, type);
            }
        }

        protected virtual void WriteArray(JsonWriter jwriter, IList list, JsonSerializer serializer)
        {
            jwriter.WriteStartArray();
            var listInfo = Serialization.Instance.GetTypeInfo(list.GetType());
            var itemType = listInfo.ListItemType;
            var itemInfo = listInfo.ListItemTypeInfo;
            foreach (var item in list)
            {
                if (item is ISynchronized isSynch && isSynch.SyncStatus == SynchronizedStatus.Actual)
                {
                    continue;
                }
                Write(jwriter, item, serializer, itemInfo);
            }
            jwriter.WriteEndArray();
        }
    }
}