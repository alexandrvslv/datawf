using System.Collections.Generic;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public class DictionaryStringKeySerializer<T, V> : DictionarySerializer<T, string, V>
    where T: IDictionary<string, V>
{
    public static readonly new DictionaryStringKeySerializer<T, V> Instance = new();

    public override void Read(ref Utf8JsonReader reader, scoped ref T? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            value = default;
            return;
        }
        value ??= TypeInfo.Constructor.Create();
        var itemSerializer = SerializeProvider.GetSerializer<V>();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if(reader.TokenType != JsonTokenType.PropertyName)
                continue;
            var key = reader.GetString()!;
            reader.Read();
            V? entry = default;
            itemSerializer.Read(ref reader, ref entry, options);
            value[key] = entry!;
        }
    }

    public override void Write(Utf8JsonWriter writer, scoped ref T? value, JsonSerializerOptions options)
    {
        if(value == null)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStartObject();

        var itemSerializer = SerializeProvider.GetSerializer<V>();

        foreach (var entry in value)
        {
            writer.WritePropertyName(entry.Key);
            var entryValue = entry.Value;
            itemSerializer.Write(writer, ref entryValue, options);
        }
        writer.WriteEndObject();
        
    }
}

