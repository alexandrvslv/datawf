using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using DataWF.Invokers.Defaults;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public class ImmutableDictionarySerializer<K, V> : GenericDictionarySerializer<ImmutableDictionary<K, V>, K, V>
    where K : notnull
{
    public static readonly ImmutableDictionarySerializer<K, V> Instance = new();

    public static new ISerializeTypeInfo<ImmutableDictionary<K, V>> GetTypeInfo()
    {
        return new SerializeTypeInfo<ImmutableDictionary<K, V>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<ImmutableDictionary<K, V>, int>(
                    invoker: ImmutableDictionaryInvokers<K, V>.CountInvoker.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance),
                new SerializePropertyInfo<ImmutableDictionary<K, V>, bool>(
                    invoker: ImmutableDictionaryInvokers<K, V>.IsEmptyInvoker.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<ImmutableDictionary<K, V>, IEnumerable<K>>(
                    invoker: ImmutableDictionaryInvokers<K, V>.KeysInvoker.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<ImmutableDictionary<K, V>, IEnumerable<V>>(
                    invoker: ImmutableDictionaryInvokers<K, V>.ValuesInvoker.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize)
            ]
        );
    }

    public ImmutableDictionarySerializer()
        : base(GetTypeInfo())
    { }

    public override void Read(XmlInvokeReader reader, ref ImmutableDictionary<K, V>? value)
    {
        (bool nullValue, int? length) = reader.ReadCollectionAttributes();
        if (nullValue)
        {
            value = default;
            return;
        }

        if (length.HasValue && length.Value == 0
            || reader.IsEmptyElement)
        {
            value = ImmutableDictionary<K, V>.Empty;
            return;
        }

        var dictionary = new Dictionary<K, V>();
        var keySerializer = KeySerializer;
        var valueSerializer = ValueSerializer;

        while (reader.ReadNextElement())
        {
            K key = default!;
            V? itemValue = default;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.Name == stringXmlKey)
                    {
                        keySerializer.Read(reader, ref key!);
                    }
                    else if (reader.Name == stringXmlValue)
                    {
                        valueSerializer.Read(reader, ref itemValue);
                    }
                }

                reader.MoveToElement();
            }
            if (!reader.IsEmptyElement)
            {
                while (reader.ReadNextElement())
                {
                    if (reader.Name == stringXmlKey)
                    {
                        keySerializer.Read(reader, ref key!);
                    }
                    else if (reader.Name == stringXmlValue)
                    {
                        valueSerializer.Read(reader, ref itemValue);
                    }
                }
            }
            dictionary.Add(key, itemValue!);
        }

        value = dictionary.ToImmutableDictionary();
    }

    public override void Write(XmlInvokeWriter writer, ref ImmutableDictionary<K, V>? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteAttributes(ref value, TypeInfo);

        var keySerializer = KeySerializer;
        var valueSerializer = ValueSerializer;

        foreach (var item in value)
        {
            item.Deconstruct(out var key, out var itemValue);
            writer.WriteStartElement("e"); //typeInfo?.ShortName

            writer.WriteStart(keySerializer.IsAttributeSerializer, stringXmlKey);
            keySerializer.Write(writer, ref key);
            writer.WriteEnd(keySerializer.IsAttributeSerializer);

            writer.WriteStart(valueSerializer.IsAttributeSerializer, stringXmlValue);
            valueSerializer.Write(writer, ref itemValue);
            writer.WriteEnd(valueSerializer.IsAttributeSerializer);

            writer.WriteEndElement();
        }
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref ImmutableDictionary<K, V>? value,
        JsonSerializerOptions options)
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

        var dictionary = new Dictionary<K, V>(length);
        var keySerializer = KeySerializer;
        var valueSerializer = ValueSerializer;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            reader.Read();
            K key = default!;
            keySerializer.Read(ref reader, ref key!, options);

            reader.Read();
            reader.Read();
            V? itemValue = default;
            valueSerializer.Read(ref reader, ref itemValue, options);

            dictionary.Add(key, itemValue!);
        }

        value = dictionary.ToImmutableDictionary();
    }

    public override void Write(Utf8JsonWriter writer, ref ImmutableDictionary<K, V>? value,
        JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        if (options.ReadCommentHandling == JsonCommentHandling.Allow)
            writer.WriteCommentCount(value.Count);

        var keySerializer = KeySerializer;
        var valueSerializer = ValueSerializer;
        writer.WriteStartArray();
        foreach (var item in value)
        {
            item.Deconstruct(out var key, out var itemValue);
            writer.WriteStartObject();
            writer.WritePropertyName(stringJsonKey);
            keySerializer.Write(writer, ref key, options);
            writer.WritePropertyName(stringJsonValue);
            valueSerializer.Write(writer, ref itemValue, options);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    public override void Read(ref MemoryPackReader reader, scoped ref ImmutableDictionary<K, V>? value)
    {
        if (!reader.TryReadCollectionHeader(out var length))
        {
            value = default;
            return;
        }

        if (length == 0)
        {
            value = ImmutableDictionary<K, V>.Empty;
            return;
        }

        var dictionary = new Dictionary<K, V>(length);
        var keySerializer = KeySerializer;
        var valueSerializer = ValueSerializer;
        var isAnyData = value?.Count > 0;
        for (int i = 0; i < length; i++)
        {
            K key = default!;
            keySerializer.Read(ref reader, ref key!);
            V? itemValue = default;
            if (isAnyData)
                value!.TryGetValue(key, out itemValue);
            valueSerializer.Read(ref reader, ref itemValue);
            dictionary.Add(key, itemValue!);
        }

        value = dictionary.ToImmutableDictionary();
    }

    override public void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer,
        scoped ref ImmutableDictionary<K, V>? value)
    {
        if (value == null)
        {
            writer.WriteNullCollectionHeader();
            return;
        }

        writer.WriteCollectionHeader(value.Count);
        var keySerializer = KeySerializer;
        var valueSerializer = ValueSerializer;

        foreach (var item in value)
        {
            item.Deconstruct(out var itemKey, out var itemValue);
            keySerializer.Write(ref writer, ref itemKey);
            valueSerializer.Write(ref writer, ref itemValue);
        }
    }

    public override void Read(ref MessagePackReader reader, scoped ref ImmutableDictionary<K, V>? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = default;
            return;
        }

        var length = reader.ReadMapHeader();
        var dictionary = new Dictionary<K, V>(length);
        var keySerializer = KeySerializer;
        var valueSerializer = ValueSerializer;
        var isAnyData = value?.Count > 0;
        for (var i = 0; i < length; i++)
        {
            K key = default!;
            keySerializer.Read(ref reader, ref key!, options);
            V? itemValue = default;
            if (isAnyData)
                value!.TryGetValue(key, out itemValue);
            valueSerializer.Read(ref reader, ref itemValue, options);
            dictionary.Add(key, itemValue!);
        }

        value = dictionary.ToImmutableDictionary();
    }

    public override void Write(ref MessagePackWriter writer, scoped ref ImmutableDictionary<K, V>? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteMapHeader(value.Count);
        var keySerializer = KeySerializer;
        var valueSerializer = ValueSerializer;
        foreach (var item in value)
        {
            item.Deconstruct(out var itemKey, out var itemValue);
            keySerializer.Write(ref writer, ref itemKey, options);
            valueSerializer.Write(ref writer, ref itemValue, options);
        }
    }
}