using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text.Json;
using DataWF.Invokers.Defaults;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class ImmutableListSerializer<T> : GenericCollectionSerializer<ImmutableList<T>, T>
{
    public static readonly ImmutableListSerializer<T> Instance = new();

    public static new ISerializeTypeInfo<ImmutableList<T>> GetTypeInfo()
    {
        return new SerializeTypeInfo<ImmutableList<T>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<ImmutableList<T>, int>(
                    invoker: ImmutableListInvokers<T>.CountInvoker.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance),
                new SerializePropertyInfo<ImmutableList<T>, bool>(
                    invoker: ImmutableListInvokers<T>.IsEmptyInvoker.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: BoolSerializer.Instance)
            ]
        );
    }

    public ImmutableListSerializer()
        : base(GetTypeInfo())
    { }

    public override void Read(XmlInvokeReader reader, ref ImmutableList<T>? value)
    {
        (bool nullValue, int? length) = reader.ReadCollectionAttributes();
        if (nullValue)
        {
            value = default;
            return;
        }

        if (length.HasValue && length.Value == 0)
        {
            value = [];
            return;
        }


        if (reader.IsEmptyElement)
        {
            value = [];
            return;
        }

        var itemSerializer = reader.Serializer.GetSerializer<T>();
        if (length.HasValue)
        {
            var list = new List<T>(length!.Value);
            CollectionsMarshal.SetCount(list, length!.Value);
            foreach (ref var entry in CollectionsMarshal.AsSpan(list))
            {
                if (!reader.ReadNextElement())
                    return;
                itemSerializer.Read(reader, ref entry);
            }
            value = [.. list];

        }
        else
        {
            var builder = ImmutableList.CreateBuilder<T>();
            while (reader.ReadNextElement())
            {
                T? entry = default;
                itemSerializer.Read(reader, ref entry);

                builder.Add(entry!);
            }
            value = builder.ToImmutableList();
        }
    }

    public override void Write(XmlInvokeWriter writer, ref ImmutableList<T>? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteAttributeString(nameof(ImmutableList<>.Count), value.Count.ToString());
        var itemSerializer = writer.Serializer.GetSerializer<T>();
        foreach (var entry in value)
        {
            var temp = entry;
            writer.Write(ref temp, itemSerializer);
        }
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref ImmutableList<T>? value, JsonSerializerOptions options)
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
            value = [];
            return;
        }


        var itemSerializer = ElementSerializer;
        if (length.HasValue)
        {
            var list = new List<T>(length.Value);
            CollectionsMarshal.SetCount(list, length.Value);
            foreach (ref var entry in CollectionsMarshal.AsSpan(list))
            {
                itemSerializer.Read(ref reader, ref entry, options);
            }
        }
        else
        {
            var builder = ImmutableList.CreateBuilder<T>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                T? entry = default;
                itemSerializer.Read(ref reader, ref entry, options);
                builder.Add(entry!);
            }
            value = builder.ToImmutableList();

        }
    }

    public override void Write(Utf8JsonWriter writer, ref ImmutableList<T>? value, JsonSerializerOptions options)
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
        foreach (var entry in value)
        {
            var temp = entry;
            itemSerializer.Write(writer, ref temp, options);
        }

        writer.WriteEndArray();
    }

    public override void Read(ref MemoryPackReader reader, scoped ref ImmutableList<T>? value)
    {
        if (!reader.TryReadCollectionHeader(out var length))
        {
            value = null;
            return;
        }
        if (length == 0)
        {
            value = [];
            return;
        }

        var list = new List<T>(length);

        var itemSerializer = ElementSerializer;
        CollectionsMarshal.SetCount(list, length);
        foreach (ref var entry in CollectionsMarshal.AsSpan(list))
        {
            itemSerializer.Read(ref reader, ref entry);
        }
        value = [.. list];
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ImmutableList<T>? value)
    {
        if (value == null)
        {
            writer.WriteNullCollectionHeader();
            return;
        }

        writer.WriteCollectionHeader(value.Count);
        var itemSerializer = ElementSerializer;
        foreach (var entry in value)
        {
            var temp = entry;
            itemSerializer.Write(ref writer, ref temp);
        }
    }

    public override void Read(ref MessagePackReader reader, scoped ref ImmutableList<T>? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = null;
            return;
        }
        var length = reader.ReadArrayHeader();
        if (length == 0)
        {
            value = [];
            return;
        }

        var list = new List<T>(length);

        var itemSerializer = ElementSerializer;
        CollectionsMarshal.SetCount(list, length);
        foreach (ref var entry in CollectionsMarshal.AsSpan(list))
        {
            itemSerializer.Read(ref reader, ref entry, options);
        }
        value = [.. list];
    }

    public override void Write(ref MessagePackWriter writer, scoped ref ImmutableList<T>? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(value.Count);
        var itemSerializer = ElementSerializer;
        foreach (var entry in value)
        {
            var temp = entry;
            itemSerializer.Write(ref writer, ref temp, options);
        }
    }
}

