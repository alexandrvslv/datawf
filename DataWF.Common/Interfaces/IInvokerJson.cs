using System.Text.Json;

namespace DataWF.Common
{
    //Boxing optimization
    public interface IInvokerJson : IInvoker
    {
        JsonEncodedText JsonName { get; }
        void WriteValue(Utf8JsonWriter writer, object value, JsonSerializerOptions option);
        void ReadValue(ref Utf8JsonReader reader, object value, JsonSerializerOptions option);
    }

    public interface IInvokerJson<T> : IInvokerJson
    {
        void WriteValue(Utf8JsonWriter writer, T value, JsonSerializerOptions option);
        void ReadValue(ref Utf8JsonReader reader, T value, JsonSerializerOptions option);
    }
}
