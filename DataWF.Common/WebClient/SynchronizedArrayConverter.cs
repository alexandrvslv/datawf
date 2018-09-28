using Newtonsoft.Json;
using System;
using System.Collections;

namespace DataWF.Common
{
    public class SynchronizedArrayConverter : JsonConverter
    {
        public static readonly SynchronizedArrayConverter Instance = new SynchronizedArrayConverter();

        public override bool CanConvert(Type objectType)
        {
            return TypeHelper.IsInterface(objectType, typeof(IEnumerable));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize(reader, objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            foreach (var item in (IEnumerable)value)
            {
                if (item is ISynchronized isSynch && (isSynch.IsSynchronized ?? false))
                {
                    continue;
                }
                serializer.Serialize(writer, item);
            }
            writer.WriteEndArray();
        }
    }
}