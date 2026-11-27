using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public class ImmutableSortedDictionaryStringKeySerializer<V> : ImmutableSortedDictionarySerializer<string, V>
{
    public static readonly new ImmutableSortedDictionaryStringKeySerializer<V> Instance = new();

    public override void Read(ref Utf8JsonReader reader, scoped ref ImmutableSortedDictionary<string, V>? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            value = default;
            return;
        }
        int length = 4;
        if (reader.TokenType == JsonTokenType.Comment)
        {
            length = int.TryParse(reader.ValueSpan, out int count) ? count : length;
            reader.Read();
        }
        var dictionary = new Dictionary<string, V>(length);
        var itemSerializer = SerializeProvider.GetSerializer<V>();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;
            var key = reader.GetString()!;
            reader.Read();

            V? entry = default;
            itemSerializer.Read(ref reader, ref entry, options);
            dictionary[key] = entry!;
        }
        value = dictionary.ToImmutableSortedDictionary();

    }

    public override void Write(Utf8JsonWriter writer, scoped ref ImmutableSortedDictionary<string, V>? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteCommentValue(value.Count.ToString());
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

