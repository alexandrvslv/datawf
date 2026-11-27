using System;
using System.Collections;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class ArrayListSerializer : ElementSerializer<ArrayList>
{
    public static readonly ArrayListSerializer Instance = new();
    public static new SerializeTypeInfo<ArrayList> GetTypeInfo()
    {
        return new SerializeTypeInfo<ArrayList>(
            constructor: null!,
            properties: []
        );
    }

    public ArrayListSerializer() : base(GetTypeInfo())
    { }

    public override bool IsAttributeSerializer => false;

    public override void Read(XmlInvokeReader reader, ref ArrayList? value)
    {
        var (nullValue, length) = reader.ReadCollectionAttributes();
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
                value = new(0);
            return;
        }

        if (value != null)
            value.Clear();
        else
            value ??= new ArrayList(length ?? 4);

        if (reader.IsEmptyElement)
        {
            return;
        }

        var listIndex = 0;
        while (reader.ReadNextElement())
        {
            var entry = listIndex < value.Count ? value[listIndex] : null;
            ObjectSerializer.Instance.Read(reader, ref entry);

            if (listIndex < value.Count)
            {
                value[listIndex] = entry;
            }
            else
            {
                value.Add(entry);
            }

            listIndex++;
        }
    }

    public override void Write(XmlInvokeWriter writer, ref ArrayList? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteAttributeString(nameof(ArrayList.Count), value.Count.ToString());
        for (int i = 0; i < value.Count; i++)
        {
            var item = value[i];
            writer.Write(ref item, ObjectSerializer.Instance!);
        }
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref ArrayList? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            return;
        }

        int length = 0;
        if (reader.TokenType == JsonTokenType.Comment)
        {
            length = int.TryParse(reader.ValueSpan, out int count) ? count : 0;
            reader.Read();
        }

        value ??= new ArrayList(length);
        var listIndex = 0;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            var entry = listIndex < value.Count ? value[listIndex] : null;
            ObjectSerializer.Instance.Read(ref reader, ref entry, options);
            if (listIndex < value.Count)
            {
                value[listIndex] = entry;
            }
            else
            {
                value.Add(entry);
            }

            listIndex++;
        }
    }

    public override void Write(Utf8JsonWriter writer, ref ArrayList? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        if (options.ReadCommentHandling == JsonCommentHandling.Allow)
            writer.WriteCommentCount(value.Count);
        writer.WriteStartArray();
        for (int i = 0; i < value.Count; i++)
        {
            var item = value[i];
            ObjectSerializer.Instance.Write(writer, ref item, options);
        }

        writer.WriteEndArray();
    }

    public override void Read(ref MemoryPackReader reader, scoped ref ArrayList? value)
    {
        if (!reader.TryReadCollectionHeader(out var length))
        {
            value = null;
            return;
        }

        value ??= new ArrayList(length);

        var listIndex = 0;
        for (int i = 0; i < length; i++)
        {
            var entry = listIndex < value.Count ? value[listIndex] : null;
            ObjectSerializer.Instance.Read(ref reader, ref entry);
            if (listIndex < value.Count)
            {
                value[listIndex] = entry;
            }
            else
            {
                value.Add(entry);
            }

            listIndex++;
        }
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ArrayList? value)
    {
        if (value == null)
        {
            writer.WriteNullCollectionHeader();
            return;
        }

        writer.WriteCollectionHeader(value.Count);
        for (int i = 0; i < value.Count; i++)
        {
            var item = value[i];
            ObjectSerializer.Instance.Write(ref writer, ref item);
        }
    }

    public override void Read(ref MessagePackReader reader, scoped ref ArrayList? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = null;
            return;
        }

        var length = reader.ReadArrayHeader();
        value ??= new ArrayList(length);
        value.Clear();
        for (var i = 0; i < length; i++)
        {
            object? item = null;
            ObjectSerializer.Instance.Read(ref reader, ref item, options);
            value.Add(item);
        }
    }

    public override void Write(ref MessagePackWriter writer, scoped ref ArrayList? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(value.Count);
        for (var i = 0; i < value.Count; i++)
        {
            var item = value[i];
            ObjectSerializer.Instance.Write(ref writer, ref item, options);
        }
    }

    public override ArrayList FromString(ReadOnlySpan<char> value) => throw new NotImplementedException();

    public override string ToString(ArrayList value) => throw new NotImplementedException();

    public override string FormatDisplay(ArrayList value, string? format = null) => throw new NotImplementedException();

    public override ArrayList Parse(object value) => throw new NotImplementedException();
}