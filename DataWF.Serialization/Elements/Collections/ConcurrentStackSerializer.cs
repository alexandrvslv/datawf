using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Xml;
using DataWF.Invokers.Defaults;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class ConcurrentStackSerializer<TElement> : GenericCollectionSerializer<ConcurrentStack<TElement>, TElement>
{
    public static readonly ConcurrentStackSerializer<TElement> Instance = new();

    public static new ISerializeTypeInfo<ConcurrentStack<TElement>> GetTypeInfo()
    {
        return new SerializeTypeInfo<ConcurrentStack<TElement>>(
            constructor: ConcurrentStackInvokers<TElement>.ConcurrentStackConstructor.Instance,
            properties: [
                new SerializePropertyInfo<ConcurrentStack<TElement>, int>(
                    invoker: ConcurrentStackInvokers<TElement>.CountInvoker<ConcurrentStack<TElement>>.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance)
            ]
        );
    }

    public ConcurrentStackSerializer()
        : base(GetTypeInfo())
    { }

    public override void Read(XmlInvokeReader reader, ref ConcurrentStack<TElement>? value)
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
            value = new ConcurrentStack<TElement>();

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
            value.Push(entry!);
            listIndex++;
        }
    }

    public override void Write(XmlInvokeWriter writer, ref ConcurrentStack<TElement>? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteAttributeString(nameof(ConcurrentStack<>.Count), value.Count.ToString());
        var itemSerializer = writer.Serializer.GetSerializer<TElement>();
        foreach (var item in value.Reverse())
        {
            var itemValue = item;
            writer.Write(ref itemValue, itemSerializer);
        }
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref ConcurrentStack<TElement>? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            return;
        }
        if (value == null)
            value = new ConcurrentStack<TElement>();
        else
            value.Clear();
        var itemSerializer = ElementSerializer;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            TElement? entry = default;
            itemSerializer.Read(ref reader, ref entry, options);
            value.Push(entry!);
        }
    }

    public override void Write(Utf8JsonWriter writer, ref ConcurrentStack<TElement>? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStartArray();
        var itemSerializer = ElementSerializer;
        foreach (var item in value.Reverse())
        {
            var itemValue = item;
            itemSerializer.Write(writer, ref itemValue, options);
        }
        writer.WriteEndArray();
    }

    public override void Read(ref MemoryPackReader reader, scoped ref ConcurrentStack<TElement>? value)
    {
        if (!reader.TryReadCollectionHeader(out var length))
        {
            value = null;
            return;
        }
        if (value == null)
            value = new ConcurrentStack<TElement>();
        else
            value.Clear();
        var itemSerializer = ElementSerializer;
        for (int i = 0; i < length; i++)
        {
            TElement? entry = default;
            itemSerializer.Read(ref reader, ref entry);
            value.Push(entry!);
        }
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ConcurrentStack<TElement>? value)
    {
        if (value == null)
        {
            writer.WriteNullCollectionHeader();
            return;
        }

        writer.WriteCollectionHeader(value.Count);
        var itemSerializer = ElementSerializer;
        foreach (var item in value.Reverse())
        {
            var itemValue = item;
            itemSerializer.Write(ref writer, ref itemValue);
        }
    }

    public override void Read(ref MessagePackReader reader, scoped ref ConcurrentStack<TElement>? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = null;
            return;
        }
        var length = reader.ReadArrayHeader();
        if (value == null)
            value = new ConcurrentStack<TElement>();
        else
            value.Clear();
        var itemSerializer = ElementSerializer;
        for (int i = 0; i < length; i++)
        {
            TElement? entry = default;
            itemSerializer.Read(ref reader, ref entry, options);
            value.Push(entry!);
        }
    }

    public override void Write(ref MessagePackWriter writer, scoped ref ConcurrentStack<TElement>? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(value.Count);
        var itemSerializer = ElementSerializer;
        foreach (var item in value.Reverse())
        {
            var itemValue = item;
            itemSerializer.Write(ref writer, ref itemValue, options);
        }
    }
}

