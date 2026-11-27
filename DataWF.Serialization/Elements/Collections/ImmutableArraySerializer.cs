using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text.Json;
using DataWF.Invokers.Defaults;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class ImmutableArraySerializer<T> : GenericCollectionSerializer<ImmutableArray<T>, T>
{
    public static readonly ImmutableArraySerializer<T> Instance = new();

    public static new ISerializeTypeInfo<ImmutableArray<T>> GetTypeInfo()
    {
        return new SerializeTypeInfo<ImmutableArray<T>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<ImmutableArray<T>, int>(
                    invoker: ImmutableArrayInvokers<T>.LengthInvoker.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance),
                new SerializePropertyInfo<ImmutableArray<T>, bool>(
                    invoker: ImmutableArrayInvokers<T>.IsEmptyInvoker.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: BoolSerializer.Instance)
            ]
        );
    }

    public ImmutableArraySerializer()
        : base(GetTypeInfo())
    { }

    public override void Read(XmlInvokeReader reader, ref ImmutableArray<T> value)
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
            var list = new T[length.Value];
            foreach (ref var entry in list.AsSpan())
            {
                if (!reader.ReadNextElement())
                    return;
                itemSerializer.Read(reader, ref entry);
            }
            value = ImmutableCollectionsMarshal.AsImmutableArray(list);
        }
        else
        {
            var list = new List<T>(length ?? 16);

            while (reader.ReadNextElement())
            {
                T? entry = default;
                itemSerializer.Read(reader, ref entry);

                list.Add(entry!);
                value = [.. list];
            }
        }
    }

    public override void Write(XmlInvokeWriter writer, ref ImmutableArray<T> value)
    {
        writer.WriteAttributeString(nameof(ImmutableArray<>.Length), value.Length.ToString());
        var itemSerializer = writer.Serializer.GetSerializer<T>();
        foreach (var entry in value)
        {
            var temp = entry;
            writer.Write(ref temp, itemSerializer);
        }
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref ImmutableArray<T> value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            value = default;
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
            var list = new T[length.Value];
            foreach (ref var entry in list.AsSpan())
            {
                itemSerializer.Read(ref reader, ref entry, options);
            }
            value = ImmutableCollectionsMarshal.AsImmutableArray(list);
        }
        else
        {
            var list = new List<T>(length ?? 16);
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                T? entry = default;
                itemSerializer.Read(ref reader, ref entry, options);
                list.Add(entry!);
            }
            value = [.. list];
        }
    }

    public override void Write(Utf8JsonWriter writer, ref ImmutableArray<T> value, JsonSerializerOptions options)
    {
        if (options.ReadCommentHandling == JsonCommentHandling.Allow)
            writer.WriteCommentCount(value.Length);
        writer.WriteStartArray();
        var itemSerializer = ElementSerializer;
        foreach (var entry in value)
        {
            var temp = entry;
            itemSerializer.Write(writer, ref temp, options);
        }

        writer.WriteEndArray();
    }

    public override void Read(ref MemoryPackReader reader, scoped ref ImmutableArray<T> value)
    {
        if (!reader.TryReadCollectionHeader(out var length))
        {
            value = default;
            return;
        }
        if (length == 0)
        {
            value = [];
            return;
        }

        var list = new T[length];

        var itemSerializer = ElementSerializer;
        foreach (ref var entry in list.AsSpan())
        {
            itemSerializer.Read(ref reader, ref entry);
        }
        value = ImmutableCollectionsMarshal.AsImmutableArray(list);
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ImmutableArray<T> value)
    {
        writer.WriteCollectionHeader(value.Length);
        var itemSerializer = ElementSerializer;
        foreach (var entry in value)
        {
            var temp = entry;
            itemSerializer.Write(ref writer, ref temp);
        }
    }

    public override void Read(ref MessagePackReader reader, scoped ref ImmutableArray<T> value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = default;
            return;
        }
        var length = reader.ReadArrayHeader();
        if (length == 0)
        {
            value = [];
            return;
        }

        var list = new T[length];

        var itemSerializer = ElementSerializer;
        foreach (ref var entry in list.AsSpan())
        {
            itemSerializer.Read(ref reader, ref entry, options);
        }
        value = ImmutableCollectionsMarshal.AsImmutableArray(list);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref ImmutableArray<T> value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(value.Length);
        var itemSerializer = ElementSerializer;
        foreach (var entry in value)
        {
            var temp = entry;
            itemSerializer.Write(ref writer, ref temp, options);
        }
    }
}
