using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using DataWF.Invokers.Defaults;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class ListSerializer<T> : GenericCollectionSerializer<List<T>, T>
{
    public static readonly ListSerializer<T> Instance = new();

    public static new ISerializeTypeInfo<List<T>> GetTypeInfo()
    {
        return new SerializeTypeInfo<List<T>>(
            constructor: ListInvokers<T>.ListConstructor.Instance,
            properties: [
                new SerializePropertyInfo<List<T>, int>(
                    invoker: ListInvokers<T>.CountInvoker<List<T>>.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance)
            ]
        );
    }

    public ListSerializer() : base(GetTypeInfo())
    { }

    public override void Read(XmlInvokeReader reader, ref List<T>? value)
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

        value ??= new List<T>(length ?? 16);

        if (reader.IsEmptyElement)
        {
            return;
        }

        var itemSerializer = reader.Serializer.GetSerializer<T>();
        if (length.HasValue)
        {
            CollectionsMarshal.SetCount(value, length.Value);
            foreach (ref var entry in CollectionsMarshal.AsSpan(value))
            {
                if (!reader.ReadNextElement())
                    return;
                itemSerializer.Read(reader, ref entry);
            }
            reader.ReadNextElement();
        }
        else
        {
            value.Clear();
            while (reader.ReadNextElement())
            {
                T? entry = default;
                itemSerializer.Read(reader, ref entry);
                value.Add(entry!);
            }
        }
    }

    public override void Write(XmlInvokeWriter writer, ref List<T>? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteAttributeString(nameof(List<>.Count), value.Count.ToString());
        var itemSerializer = writer.Serializer.GetSerializer<T>();
        foreach (ref var entry in CollectionsMarshal.AsSpan(value))
        {
            writer.Write(ref entry, itemSerializer);
        }
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref List<T>? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            return;
        }

        int? length = null;
        if (reader.TokenType == JsonTokenType.Comment)
        {
            length = int.TryParse(reader.ValueSpan, out int count) ? count : null;
            reader.Read();
        }

        if (length.HasValue && length.Value == 0)
        {
            reader.Read();
            if (value != null)
                value.Clear();
            else
                value = new();
            return;
        }

        value ??= new List<T>(length ?? 16);

        var itemSerializer = ElementSerializer;
        if (length.HasValue)
        {
            CollectionsMarshal.SetCount(value, length.Value);
            foreach (ref var entry in CollectionsMarshal.AsSpan(value))
            {
                itemSerializer.Read(ref reader, ref entry, options);
            }
        }
        else
        {
            value.Clear();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                T? entry = default;
                itemSerializer.Read(ref reader, ref entry, options);
                value.Add(entry!);
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, ref List<T>? value, JsonSerializerOptions options)
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
        foreach (ref var entry in CollectionsMarshal.AsSpan(value))
        {
            itemSerializer.Write(writer, ref entry, options);
        }

        writer.WriteEndArray();
    }

    public override void Read(ref MemoryPackReader reader, scoped ref List<T>? value)
    {
        if (!reader.TryReadCollectionHeader(out var length))
        {
            value = null;
            return;
        }
        if (length == 0)
        {
            if (value == null)
                value = new List<T>();
            else
                value.Clear();
            return;
        }


        value ??= new List<T>(length);
        CollectionsMarshal.SetCount(value, length);
        if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            var span = CollectionsMarshal.AsSpan(value);
            reader.ReadSpanWithoutReadLengthHeader<T>(length, ref span!);
            return;
        }
        var itemSerializer = ElementSerializer;
        foreach (ref var entry in CollectionsMarshal.AsSpan(value))
        {
            itemSerializer.Read(ref reader, ref entry);
        }
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref List<T>? value)
    {
        if (value == null)
        {
            writer.WriteNullCollectionHeader();
            return;
        }
        if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            var span = CollectionsMarshal.AsSpan(value);
            writer.DangerousWriteUnmanagedSpan<T>(span);
            return;
        }

        writer.WriteCollectionHeader(value.Count);
        var itemSerializer = ElementSerializer;
        foreach (ref var entry in CollectionsMarshal.AsSpan(value))
        {
            itemSerializer.Write(ref writer, ref entry);
        }
    }

    public override void Read(ref MessagePackReader reader, scoped ref List<T>? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = null;
            return;
        }
        var length = reader.ReadArrayHeader();
        if (length == 0)
        {
            if (value == null)
                value = new List<T>();
            else
                value.Clear();
            return;
        }

        value ??= new List<T>(length);

        var itemSerializer = ElementSerializer;
        CollectionsMarshal.SetCount(value, length);
        foreach (ref var entry in CollectionsMarshal.AsSpan(value))
        {
            itemSerializer.Read(ref reader, ref entry, options);
        }
    }

    public override void Write(ref MessagePackWriter writer, scoped ref List<T>? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(value.Count);
        var itemSerializer = ElementSerializer;
        foreach (ref var entry in CollectionsMarshal.AsSpan(value))
        {
            itemSerializer.Write(ref writer, ref entry, options);
        }
    }
}
