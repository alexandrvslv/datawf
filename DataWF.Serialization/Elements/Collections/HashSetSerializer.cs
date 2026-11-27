using System.Collections.Generic;
using System.Text.Json;
using System.Xml;
using DataWF.Invokers.Defaults;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class HashSetSerializer<T> : GenericCollectionSerializer<HashSet<T>, T>
{
    public static readonly HashSetSerializer<T> Instance = new();

    public static new ISerializeTypeInfo<HashSet<T>> GetTypeInfo()
    {
        return new SerializeTypeInfo<HashSet<T>>(
            constructor: HashSetInvokers<T>.HashSetConstructor.Instance,
            properties: [
                new SerializePropertyInfo<HashSet<T>, int>(
                    invoker: HashSetInvokers<T>.CountInvoker<HashSet<T>>.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly)
            ]
        );
    }

    public HashSetSerializer() : base(GetTypeInfo())
    { }

    public override void Read(XmlInvokeReader reader, ref HashSet<T>? value)
    {
        (bool nullValue, int? length) = reader.ReadCollectionAttributes();
        if (nullValue)
        {
            value = null;
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
            value = new HashSet<T>(length ?? 16);

        if (reader.IsEmptyElement)
        {
            return;
        }

        var itemSerializer = reader.Serializer.GetSerializer<T>();
        var listIndex = 0;
        while (reader.ReadNextElement())
        {
            T? entry = default;
            itemSerializer.Read(reader, ref entry);
            value.Add(entry!);
            listIndex++;
        }
    }

    public override void Write(XmlInvokeWriter writer, ref HashSet<T>? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteAttributeString(nameof(HashSet<>.Count), value.Count.ToString());
        var itemSerializer = writer.Serializer.GetSerializer<T>();
        foreach (var item in value)
        {
            var itemValue = item;
            writer.Write(ref itemValue, itemSerializer);
        }
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref HashSet<T>? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            return;
        }

        int? length = null;
        if (reader.TokenType == JsonTokenType.Comment)
        {
            length = int.TryParse(reader.ValueSpan, out int count) ? count : 0;
            reader.Read();
        }

        if (value == null)
            value = new HashSet<T>(length ?? 16);
        else
            value.Clear();
        var itemSerializer = ElementSerializer;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            T? entry = default;
            itemSerializer.Read(ref reader, ref entry, options);
            value.Add(entry!);
        }
    }

    public override void Write(Utf8JsonWriter writer, ref HashSet<T>? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        if (options.ReadCommentHandling == JsonCommentHandling.Allow)
            writer.WriteCommentCount(value.Count);
        writer.WriteStartArray();
        var itemSerializer = ElementSerializer;
        foreach (var item in value)
        {
            var itemValue = item;
            itemSerializer.Write(writer, ref itemValue, options);
        }

        writer.WriteEndArray();
    }

    public override void Read(ref MemoryPackReader reader, scoped ref HashSet<T>? value)
    {
        if (!reader.TryReadCollectionHeader(out var length))
        {
            value = null;
            return;
        }
        if (value == null)
            value = new HashSet<T>(length);
        else
            value.Clear();
        var itemSerializer = ElementSerializer;
        for (int i = 0; i < length; i++)
        {
            T? entry = default;
            itemSerializer.Read(ref reader, ref entry);
            value.Add(entry!);
        }
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref HashSet<T>? value)
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

    public override void Read(ref MessagePackReader reader, scoped ref HashSet<T>? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = null;
            return;
        }
        var length = reader.ReadArrayHeader();
        if (value == null)
            value = new HashSet<T>(length);
        else
            value.Clear();
        var itemSerializer = ElementSerializer;
        for (int i = 0; i < length; i++)
        {
            T? entry = default;
            itemSerializer.Read(ref reader, ref entry, options);
            value.Add(entry!);
        }
    }

    public override void Write(ref MessagePackWriter writer, scoped ref HashSet<T>? value, MessagePackSerializerOptions options)
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