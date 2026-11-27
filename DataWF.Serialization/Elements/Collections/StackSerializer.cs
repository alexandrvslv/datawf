using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml;
using DataWF.Invokers.Defaults;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class StackSerializer<TElement> : GenericCollectionSerializer<Stack<TElement>, TElement>
{
    public static readonly StackSerializer<TElement> Instance = new();

    public static new ISerializeTypeInfo<Stack<TElement>> GetTypeInfo()
    {
        return new SerializeTypeInfo<Stack<TElement>>(
            constructor: StackInvokers<TElement>.StackConstructor.Instance,
            properties: [
                new SerializePropertyInfo<Stack<TElement>, int>(
                    invoker: StackInvokers<TElement>.CountInvoker<Stack<TElement>>.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance)
            ]
        );
    }

    public StackSerializer()
        : base(GetTypeInfo())
    { }

    public override void Read(XmlInvokeReader reader, ref Stack<TElement>? value)
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
            value = new Stack<TElement>(length ?? 16);

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

    public override void Write(XmlInvokeWriter writer, ref Stack<TElement>? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteAttributeString(nameof(Stack<>.Count), value.Count.ToString());
        var itemSerializer = writer.Serializer.GetSerializer<TElement>();
        foreach (var item in value.Reverse())
        {
            var itemValue = item;
            writer.Write(ref itemValue, itemSerializer);
        }
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref Stack<TElement>? value,
        JsonSerializerOptions options)
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

        if (value != null)
            value.Clear();
        else
            value = new Stack<TElement>(length ?? 16);

        var itemSerializer = ElementSerializer;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            TElement? entry = default(TElement);
            itemSerializer.Read(ref reader, ref entry, options);
            value.Push(entry!);
        }
    }

    public override void Write(Utf8JsonWriter writer, ref Stack<TElement>? value, JsonSerializerOptions options)
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
        foreach (var item in value.Reverse())
        {
            var itemValue = item;
            itemSerializer.Write(writer, ref itemValue, options);
        }

        writer.WriteEndArray();
    }

    public override void Read(ref MemoryPackReader reader, scoped ref Stack<TElement>? value)
    {
        if (!reader.TryReadCollectionHeader(out var length))
        {
            value = null;
            return;
        }

        if (value != null)
            value.Clear();
        else
            value = new Stack<TElement>(length);

        var itemSerializer = ElementSerializer;
        for (int i = 0; i < length; i++)
        {
            TElement? entry = default;
            itemSerializer.Read(ref reader, ref entry);
            value.Push(entry!);
        }
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer,
        scoped ref Stack<TElement>? value)
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

    public override void Read(ref MessagePackReader reader, scoped ref Stack<TElement>? value, MessagePackSerializerOptions options)
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
            value = new Stack<TElement>(length);

        var itemSerializer = ElementSerializer;
        for (int i = 0; i < length; i++)
        {
            TElement? entry = default;
            itemSerializer.Read(ref reader, ref entry, options);
            value.Push(entry!);
        }
    }

    public override void Write(ref MessagePackWriter writer, scoped ref Stack<TElement>? value, MessagePackSerializerOptions options)
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