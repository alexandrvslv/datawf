using System.Text.Json;

namespace DataWF.Common
{
    public interface IClientConverter
    {
        ICrudClient Client { get; }

        TypeSerializationInfo SerializationInfo { get; }

        object Read(ref Utf8JsonReader jreader, object item, JsonSerializerOptions options);
    }
}