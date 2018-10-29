namespace DataWF.Common
{
    public struct AccessView
    {
        public bool View { get; set; }

        public bool Edit { get; set; }

        public bool Create { get; set; }

        public bool Delete { get; set; }

        public bool Admin { get; set; }

        public bool Accept { get; set; }
    }

    //public class AccessValueJson : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType)
    //    {
    //        return objectType == typeof(AccessValue);
    //    }

    //    public override bool CanRead { get { return false; } }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        if (!(value is AccessValue typeValue))
    //        {
    //            throw new JsonSerializationException($"Expected {nameof(AccessValue)} but {nameof(value)} is {value?.GetType().Name}.");
    //        }
    //        writer.WriteValue(value.ToString());
    //    }
    //}
}
