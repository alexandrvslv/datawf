using System;
using System.Collections.Generic;
using System.Text.Json;
using DataWF.Invokers.Defaults;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public class ArraySerializer<T> : ElementSerializer<T[]?>
{
    public static readonly ArraySerializer<T> ArrInstance = new();

    public static new SerializeTypeInfo<T[]?> GetTypeInfo()
    {
        return new SerializeTypeInfo<T[]?>(
            constructor: ArrayInvokers<T>.ArrayConstructor.Instance,
            properties:
            [
                new SerializePropertyInfo<T[]?, int>(
                    invoker: ArrayInvokers<T>.LengthInvoker.Instance,
                    keys: SerializePropertyKeys.ReadOnlyNonSerializeAttribute),
                new SerializePropertyInfo<T[]?, long>(
                    invoker: ArrayInvokers<T>.LongLengthInvoker.Instance,
                    keys: SerializePropertyKeys.ReadOnlyNonSerializeAttribute),
                new SerializePropertyInfo<T[]?, int>(
                    invoker: ArrayInvokers<T>.RankInvoker.Instance,
                    keys: SerializePropertyKeys.ReadOnlyNonSerializeAttribute)
            ]
        );
    }

    IElementSerializer<T> elementInfo;

    public ArraySerializer()
        : base(GetTypeInfo())
    {
        elementInfo = SerializeProvider.GetSerializer<T>();
    }

    public override bool IsAttributeSerializer => false;

    protected IElementSerializer<T> ElementSerializer => elementInfo;

    public override string FormatDisplay(T[]? value, string? format = null)
    {
        if (value == null)
            return stringNull;
        var text = new System.Text.StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            if (i > 0)
                text.Append(", ");
            text.Append(ElementSerializer.FormatDisplay(value[i], format));
        }

        return text.ToString();
    }

    public override T[]? FromString(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return Array.Empty<T>();
        if (value.StartsWith('[') && value.EndsWith(']'))
        {
            return JsonSerializer.Deserialize<T[]>(value.ToString(), new JsonSerializerOptions
            {
                Converters = { this }
            });
        }

        throw new NotImplementedException();
    }

    public override string ToString(T[]? value)
    {
        return JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            Converters = { this }
        });
    }

    public override T[]? Parse(object value)
    {
        return value switch
        {
            T[] same => same,
            string str => FromString(str),
            null => default,
            _ => throw new NotImplementedException($"Cannot parse {value.GetType()} to {typeof(T[])}"),
        };
    }

    public override void Read(XmlInvokeReader reader, scoped ref T[]? value)
    {
        (bool nullValue, int? length) = reader.ReadCollectionAttributes();
        if (nullValue)
        {
            value = default;
            return;
        }

        if (length.HasValue && length.Value == 0)
        {
            value = Array.Empty<T>();
            return;
        }

        var itemSerializer = reader.Serializer.GetSerializer<T>();
        if (length.HasValue)
        {
            value = value == null || value.Length != length ? new T[length.Value] : value;
            var index = 0;
            while (reader.ReadNextElement())
            {
                itemSerializer.Read(reader, ref value[index++]!);
            }
        }
        else
        {
            var list = new List<T>();
            while (reader.ReadNextElement())
            {
                T? item = default;
                itemSerializer.Read(reader, ref item);
                list.Add(item!);
            }

            value = [.. list];
        }
    }

    public override void Write(XmlInvokeWriter writer, scoped ref T[]? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteAttributeString(nameof(Array.Length), value.Length.ToString());
        var itemSerializer = writer.Serializer.GetSerializer<T>();
        for (var i = 0; i < value.Length; i++)
        {
            writer.Write(ref value[i]!, itemSerializer);
        }
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref T[]? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            return;
        }

        int? length = null;
        if (reader.TokenType == JsonTokenType.Comment)
        {
            length = int.TryParse(reader.ValueSpan, out var count) ? count : null;
            reader.Read();
        }

        if (length.HasValue && length.Value == 0)
        {
            value = Array.Empty<T>();
            return;
        }

        var itemSerializer = ElementSerializer;
        if (length.HasValue)
        {
            value = value == null || value.Length != length ? new T[length.Value] : value;
            var index = 0;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                itemSerializer.Read(ref reader, ref value[index++]!, options);
            }
        }
        else
        {
            var list = new List<T>(16);
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                var element = default(T);
                itemSerializer.Read(ref reader, ref element, options);
                list.Add(element!);
            }
            value = [.. list];
        }
    }

    public override void Write(Utf8JsonWriter writer, scoped ref T[]? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        if (options.ReadCommentHandling == JsonCommentHandling.Allow)
            writer.WriteCommentCount(value.Length);
        writer.WriteStartArray();
        var itemSerializer = ElementSerializer;
        for (var i = 0; i < value.Length; i++)
        {
            itemSerializer.Write(writer, ref value[i]!, options);
        }

        writer.WriteEndArray();
    }

    public override void Read(ref MemoryPackReader reader, scoped ref T[]? value)
    {
        if (!reader.TryReadCollectionHeader(out var length))
        {
            value = null;
            return;
        }

        value = value == null || value.Length != length ? new T[length] : value;
        var itemSerializer = ElementSerializer;
        for (int i = 0; i < length; i++)
        {
            itemSerializer.Read(ref reader, ref value[i]!);
        }
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T[]? value)
    {
        if (value == null)
        {
            writer.WriteNullCollectionHeader();
            return;
        }

        writer.WriteCollectionHeader(value.Length);
        var itemSerializer = ElementSerializer;
        for (int i = 0; i < value.Length; i++)
        {
            itemSerializer.Write(ref writer, ref value[i]!);
        }
    }

    public override void Read(ref MessagePackReader reader, scoped ref T[]? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = null;
            return;
        }

        var length = reader.ReadArrayHeader();
        value = value == null || value.Length != length ? new T[length] : value;
        var elementSerializer = ElementSerializer;
        for (var i = 0; i < length; i++)
            elementSerializer.Read(ref reader, ref value[i]!, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref T[]? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(value.Length);
        var elementSerializer = ElementSerializer;
        for (var i = 0; i < value.Length; i++)
            elementSerializer.Write(ref writer, ref value[i]!, options);
    }
}