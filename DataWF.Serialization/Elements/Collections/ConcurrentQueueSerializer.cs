using System.Collections.Concurrent;
using System.Text.Json;
using System.Xml;
using DataWF.Invokers.Defaults;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class ConcurrentQueueSerializer<TElement> : GenericCollectionSerializer<ConcurrentQueue<TElement>, TElement>
{
    public static readonly ConcurrentQueueSerializer<TElement> Instance = new();

    public static new ISerializeTypeInfo<ConcurrentQueue<TElement>> GetTypeInfo()
    {
        return new SerializeTypeInfo<ConcurrentQueue<TElement>>(
            constructor: ConcurrentQueueInvokers<TElement>.ConcurrentQueueConstructor.Instance,
            properties: [
                new SerializePropertyInfo<ConcurrentQueue<TElement>, int>(
                    invoker: ConcurrentQueueInvokers<TElement>.CountInvoker<ConcurrentQueue<TElement>>.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance)
            ]
        );
    }

    public ConcurrentQueueSerializer() : base(GetTypeInfo())
    { }

    public override void Read(XmlInvokeReader reader, ref ConcurrentQueue<TElement>? value)
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
                value = new();
            return;
        }
        if (value != null)
            value.Clear();
        else
            value = new ConcurrentQueue<TElement>();

        if (reader.IsEmptyElement)
        {
            return;
        }
        var itemSerializer = reader.Serializer.GetSerializer<TElement>();
        var listIndex = 0;
        while (reader.ReadNextElement())
        {
            TElement? entry = default;
            itemSerializer.Read(reader, ref entry);
            value.Enqueue(entry!);
            listIndex++;
        }
    }

    public override void Write(XmlInvokeWriter writer, ref ConcurrentQueue<TElement>? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteAttributeString(nameof(ConcurrentQueue<>.Count), value!.Count.ToString());
        var itemSerializer = writer.Serializer.GetSerializer<TElement>();
        foreach (var item in value)
        {
            var itemValue = item;
            writer.Write(ref itemValue, itemSerializer);
        }
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref ConcurrentQueue<TElement>? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            return;
        }
        if (value == null)
            value = new ConcurrentQueue<TElement>();
        else
            value.Clear();
        var itemSerializer = ElementSerializer;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            TElement? entry = default;
            itemSerializer.Read(ref reader, ref entry, options);
            value.Enqueue(entry!);
        }
    }

    public override void Write(Utf8JsonWriter writer, ref ConcurrentQueue<TElement>? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStartArray();
        var itemSerializer = ElementSerializer;
        foreach (var item in value)
        {
            var itemValue = item;
            itemSerializer.Write(writer, ref itemValue, options);
        }
        writer.WriteEndArray();
    }

    public override void Read(ref MemoryPackReader reader, scoped ref ConcurrentQueue<TElement>? value)
    {
        if (!reader.TryReadCollectionHeader(out var length))
        {
            value = null;
            return;
        }
        if (value == null)
            value = new ConcurrentQueue<TElement>();
        else
            value.Clear();
        var itemSerializer = ElementSerializer;
        for (int i = 0; i < length; i++)
        {
            TElement? entry = default;
            itemSerializer.Read(ref reader, ref entry);
            value.Enqueue(entry!);
        }
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ConcurrentQueue<TElement>? value)
    {
        if (value == null)
        {
            writer.WriteNullCollectionHeader();
            return;
        }
        writer.WriteCollectionHeader(value.Count);
        var itemSerializer = ElementSerializer;
        foreach (var item in value)
        {
            var itemValue = item;
            itemSerializer.Write(ref writer, ref itemValue);
        }
    }

    public override void Read(ref MessagePackReader reader, scoped ref ConcurrentQueue<TElement>? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = null;
            return;
        }
        var length = reader.ReadArrayHeader();
        if (value == null)
            value = new ConcurrentQueue<TElement>();
        else
            value.Clear();
        var itemSerializer = ElementSerializer;
        for (int i = 0; i < length; i++)
        {
            TElement? entry = default;
            itemSerializer.Read(ref reader, ref entry, options);
            value.Enqueue(entry!);
        }
    }

    public override void Write(ref MessagePackWriter writer, scoped ref ConcurrentQueue<TElement>? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }
        writer.WriteArrayHeader(value.Count);
        var itemSerializer = ElementSerializer;
        foreach (var item in value)
        {
            var itemValue = item;
            itemSerializer.Write(ref writer, ref itemValue, options);
        }
    }
}

