using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using DataWF.Invokers.Defaults;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public class DictionarySerializer<T, K, V> : GenericDictionarySerializer<T, K, V>
    where T : IDictionary<K, V>
    where K : notnull
{
    public static readonly DictionarySerializer<T, K, V> Instance = new();

    public static new ISerializeTypeInfo<T> GetTypeInfo()
    {
        var type = typeof(T);
        if (type == typeof(Dictionary<K, V>))
            return (SerializeTypeInfo<T>)(object)new SerializeTypeInfo<Dictionary<K, V>>(
                constructor: DictionaryInvokers<K, V>.DictionaryConstructorInt32.Instance,
                properties: [
                    new SerializePropertyInfo<Dictionary<K, V>, int>(
                    invoker: DictionaryInvokers<K, V>.CountInvoker<Dictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance),
                new SerializePropertyInfo<Dictionary<K, V>, int>(
                    invoker: DictionaryInvokers<K, V>.CapacityInvoker<Dictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize,
                    serializer: Int32Serializer.Instance),
                new SerializePropertyInfo<Dictionary<K, V>, IEqualityComparer<K>>(
                    invoker: DictionaryInvokers<K, V>.ComparerInvoker<Dictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<Dictionary<K, V>, Dictionary<K, V>.KeyCollection>(
                    invoker: DictionaryInvokers<K, V>.KeysInvoker<Dictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<Dictionary<K, V>, Dictionary<K, V>.ValueCollection>(
                    invoker: DictionaryInvokers<K, V>.ValuesInvoker<Dictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize)
                ]
            );
        else if (type == typeof(SortedDictionary<K, V>))
            return (SerializeTypeInfo<T>)(object)new SerializeTypeInfo<SortedDictionary<K, V>>(
                constructor: SortedDictionaryInvokers<K, V>.SortedDictionaryConstructor.Instance,
                properties: [
                new SerializePropertyInfo<SortedDictionary<K, V>, int>(
                    invoker: SortedDictionaryInvokers<K, V>.CountInvoker<SortedDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance),
                new SerializePropertyInfo<SortedDictionary<K, V>, IComparer<K>>(
                    invoker: SortedDictionaryInvokers<K, V>.ComparerInvoker<SortedDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<SortedDictionary<K, V>, SortedDictionary<K, V>.KeyCollection>(
                    invoker: SortedDictionaryInvokers<K, V>.KeysInvoker<SortedDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<SortedDictionary<K, V>, SortedDictionary<K, V>.ValueCollection>(
                    invoker: SortedDictionaryInvokers<K, V>.ValuesInvoker<SortedDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize)
                ]
            );
        else if (type == typeof(SortedList<K, V>))
            return (SerializeTypeInfo<T>)(object)new SerializeTypeInfo<SortedList<K, V>>(
                constructor: SortedListInvokers<K, V>.SortedListConstructorInt32.Instance,
                properties: [
                new SerializePropertyInfo<SortedList<K, V>, int>(
                    invoker: SortedListInvokers<K, V>.CountInvoker<SortedList<K, V>>.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance),
                new SerializePropertyInfo<SortedList<K, V>, int>(
                    invoker: SortedListInvokers<K, V>.CapacityInvoker<SortedList<K, V>>.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize,
                    serializer: Int32Serializer.Instance),
                new SerializePropertyInfo<SortedList<K, V>, IComparer<K>>(
                    invoker: SortedListInvokers<K, V>.ComparerInvoker<SortedList<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<SortedList<K, V>, IList<K>>(
                    invoker: SortedListInvokers<K, V>.KeysInvoker<SortedList<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<SortedList<K, V>, IList<V>>(
                    invoker: SortedListInvokers<K, V>.ValuesInvoker<SortedList<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize)
                ]
            );
        else if (type == typeof(ConcurrentDictionary<K, V>))
            return (SerializeTypeInfo<T>)(object)new SerializeTypeInfo<ConcurrentDictionary<K, V>>(
                constructor: ConcurrentDictionaryInvokers<K, V>.ConcurrentDictionaryConstructor.Instance,
                properties: [
                new SerializePropertyInfo<ConcurrentDictionary<K, V>, int>(
                    invoker: ConcurrentDictionaryInvokers<K, V>.CountInvoker<ConcurrentDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance),
                new SerializePropertyInfo<ConcurrentDictionary<K, V>, bool>(
                    invoker: ConcurrentDictionaryInvokers<K, V>.IsEmptyInvoker<ConcurrentDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<ConcurrentDictionary<K, V>, IEqualityComparer<K>>(
                    invoker: ConcurrentDictionaryInvokers<K, V>.ComparerInvoker<ConcurrentDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<ConcurrentDictionary<K, V>, ICollection<K>>(
                    invoker: ConcurrentDictionaryInvokers<K, V>.KeysInvoker<ConcurrentDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<ConcurrentDictionary<K, V>, ICollection<V>>(
                    invoker: ConcurrentDictionaryInvokers<K, V>.ValuesInvoker<ConcurrentDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize)
                ]
            );
        throw new NotSupportedException();
    }

    public DictionarySerializer()
        : base(GetTypeInfo())
    { }

    public DictionarySerializer(ISerializeTypeInfo<T> typeInfo)
        : base(typeInfo)
    { }

    public override void Read(XmlInvokeReader reader, ref T? value)
    {
        (bool nullValue, int? length) = reader.ReadCollectionAttributes();
        if (nullValue)
        {
            value = default;
            return;
        }

        if (length.HasValue && length.Value == 0)
        {
            if (value != null)
                value.Clear();
            else
                value = TypeInfo.Constructor.Create(0);
            return;
        }

        if (value != null)
            value.Clear();
        else
            value = TypeInfo.Constructor.Create(length ?? 4);

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
            value.Add(key, itemValue!);
        }
    }

    public override void Write(XmlInvokeWriter writer, ref T? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteAttributeString(nameof(IDictionary<,>.Count), value.Count.ToString());

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

    public override void Read(ref Utf8JsonReader reader, scoped ref T? value, JsonSerializerOptions options)
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

        value ??= TypeInfo.Constructor.Create(length);
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

            value.Add(key, itemValue!);
        }
    }

    public override void Write(Utf8JsonWriter writer, ref T? value, JsonSerializerOptions options)
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

    public override void Read(ref MemoryPackReader reader, scoped ref T? value)
    {
        if (!reader.TryReadCollectionHeader(out var length))
        {
            value = default;
            return;
        }

        value ??= TypeInfo.Constructor.Create(length);
        var keySerializer = KeySerializer;
        var valueSerializer = ValueSerializer;

        for (int i = 0; i < length; i++)
        {
            K key = default!;
            keySerializer.Read(ref reader, ref key!);
            V? itemValue = default;
            valueSerializer.Read(ref reader, ref itemValue);
            value.Add(key, itemValue!);
        }
    }

    override public void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T? value)
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
            item.Deconstruct(out var key, out var itemValue);
            keySerializer.Write(ref writer, ref key);
            valueSerializer.Write(ref writer, ref itemValue!);
        }
    }

    public override void Read(ref MessagePackReader reader, scoped ref T? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = default;
            return;
        }

        var length = reader.ReadMapHeader();
        value ??= TypeInfo.Constructor.Create(length);
        value.Clear();
        var keySerializer = KeySerializer;
        var valueSerializer = ValueSerializer;
        for (var i = 0; i < length; i++)
        {
            K key = default!;
            keySerializer.Read(ref reader, ref key!, options);
            V? itemValue = default;
            valueSerializer.Read(ref reader, ref itemValue, options);
            value.Add(key, itemValue!);
        }
    }

    public override void Write(ref MessagePackWriter writer, scoped ref T? value, MessagePackSerializerOptions options)
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
            item.Deconstruct(out var key, out var itemValue);
            keySerializer.Write(ref writer, ref key, options);
            valueSerializer.Write(ref writer, ref itemValue!, options);
        }
    }


}