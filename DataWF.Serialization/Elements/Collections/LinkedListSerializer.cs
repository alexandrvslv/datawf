using System.Collections.Generic;
using System.Text.Json;
using System.Xml;
using DataWF.Invokers.Defaults;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class LinkedListSerializer<T> : GenericCollectionSerializer<LinkedList<T>, T>
{
    public static readonly LinkedListSerializer<T> Instance = new();

    public static new ISerializeTypeInfo<LinkedList<T>> GetTypeInfo()
    {
        return new SerializeTypeInfo<LinkedList<T>>(
            constructor: LinkedListInvokers<T>.LinkedListConstructor.Instance,
            properties: [
                new SerializePropertyInfo<LinkedList<T>, int>(
                    invoker: LinkedListInvokers<T>.CountInvoker<LinkedList<T>>.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance)
            ]
        );
    }

    public LinkedListSerializer() : base(GetTypeInfo())
    { }

    public override void Read(XmlInvokeReader reader, ref LinkedList<T>? value)
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
            value = new LinkedList<T>();

        if (reader.IsEmptyElement)
        {
            return;
        }
        var itemSerializer = reader.Serializer.GetSerializer<T>();
        while (reader.ReadNextElement())
        {
            T? entry = default;
            itemSerializer.Read(reader, ref entry);
            value.AddLast(entry!);
        }
    }

    public override void Write(XmlInvokeWriter writer, ref LinkedList<T>? value)
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

    public override void Read(ref Utf8JsonReader reader, scoped ref LinkedList<T>? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            return;
        }
        if (value == null)
            value = new LinkedList<T>();
        else
            value.Clear();
        var itemSerializer = ElementSerializer;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            T? entry = default;
            itemSerializer.Read(ref reader, ref entry, options);
            value.AddLast(entry!);
        }
    }

    public override void Write(Utf8JsonWriter writer, ref LinkedList<T>? value, JsonSerializerOptions options)
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

    public override void Read(ref MemoryPackReader reader, scoped ref LinkedList<T>? value)
    {
        if (!reader.TryReadCollectionHeader(out var length))
        {
            value = null;
            return;
        }
        if (value == null)
            value = new LinkedList<T>();
        else
            value.Clear();
        var itemSerializer = ElementSerializer;
        for (int i = 0; i < length; i++)
        {
            T? entry = default;
            itemSerializer.Read(ref reader, ref entry);
            value.AddLast(entry!);
        }
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref LinkedList<T>? value)
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

    public override void Read(ref MessagePackReader reader, scoped ref LinkedList<T>? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = null;
            return;
        }
        var length = reader.ReadArrayHeader();
        if (value == null)
            value = new LinkedList<T>();
        else
            value.Clear();
        var itemSerializer = ElementSerializer;
        for (int i = 0; i < length; i++)
        {
            T? entry = default;
            itemSerializer.Read(ref reader, ref entry, options);
            value.AddLast(entry!);
        }
    }

    public override void Write(ref MessagePackWriter writer, scoped ref LinkedList<T>? value, MessagePackSerializerOptions options)
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