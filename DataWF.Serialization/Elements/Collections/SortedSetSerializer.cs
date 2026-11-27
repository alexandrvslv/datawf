using System.Collections.Generic;
using System.Text.Json;
using System.Xml;
using DataWF.Invokers.Defaults;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class SortedSetSerializer<T> : GenericCollectionSerializer<SortedSet<T>, T>
{
    public static readonly SortedSetSerializer<T> Instance = new();

    public static new ISerializeTypeInfo<SortedSet<T>> GetTypeInfo()
    {
        return new SerializeTypeInfo<SortedSet<T>>(
            constructor: SortedSetInvokers<T>.SortedSetConstructor.Instance,
            properties: [
                new SerializePropertyInfo<SortedSet<T>, int>(
                    invoker: SortedSetInvokers<T>.CountInvoker<SortedSet<T>>.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance)
            ]
        );
    }

    public SortedSetSerializer() : base(GetTypeInfo())
    { }

    public override void Read(XmlInvokeReader reader, ref SortedSet<T>? value)
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
            value = new SortedSet<T>();

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

    public override void Write(XmlInvokeWriter writer, ref SortedSet<T>? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        var itemSerializer = writer.Serializer.GetSerializer<T>();
        foreach (var item in value)
        {
            var itemValue = item;
            writer.Write(ref itemValue, itemSerializer);
        }
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref SortedSet<T>? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            return;
        }

        if (value != null)
            value.Clear();
        else
            value = new SortedSet<T>();

        var itemSerializer = ElementSerializer;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            T? entry = default;
            itemSerializer.Read(ref reader, ref entry, options);
            value.Add(entry!);
        }
    }

    public override void Write(Utf8JsonWriter writer, ref SortedSet<T>? value, JsonSerializerOptions options)
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

    public override void Read(ref MemoryPackReader reader, scoped ref SortedSet<T>? value)
    {
        if (!reader.TryReadCollectionHeader(out var length))
        {
            value = null;
            return;
        }

        if (value != null)
            value.Clear();
        else
            value = new SortedSet<T>();

        var itemSerializer = ElementSerializer;
        for (int i = 0; i < length; i++)
        {
            T? entry = default;
            itemSerializer.Read(ref reader, ref entry);
            value.Add(entry!);
        }
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SortedSet<T>? value)
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

    public override void Read(ref MessagePackReader reader, scoped ref SortedSet<T>? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = null;
            return;
        }

        var length = reader.ReadArrayHeader();
        if (value != null)
            value.Clear();
        else
            value = new SortedSet<T>();

        var itemSerializer = ElementSerializer;
        for (int i = 0; i < length; i++)
        {
            T? entry = default;
            itemSerializer.Read(ref reader, ref entry, options);
            value.Add(entry!);
        }
    }

    public override void Write(ref MessagePackWriter writer, scoped ref SortedSet<T>? value, MessagePackSerializerOptions options)
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


