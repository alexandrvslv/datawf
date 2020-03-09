
namespace DataWF.Common
{
    public interface IClientConverter
    {
        ICrudClient Client { get; }

        TypeSerializationInfo SerializationInfo { get; }
#if NETSTANDARD2_0
        object Read(Newtonsoft.Json.JsonReader jreader, object item, Newtonsoft.Json.JsonSerializer serializer);
#else
        object Read(ref System.Text.Json.Utf8JsonReader jreader, object item, System.Text.Json.JsonSerializerOptions options);
#endif
    }
}