using System;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public class AbstractSerializer<T> : ElementSerializer<T>
{
    public AbstractSerializer(ISerializeTypeInfo<T> typeInfo)
        : base(typeInfo)
    { }

    public override bool IsAttributeSerializer => TypeInfo.IsAttribute;

    protected virtual IElementSerializer? FallbackTypeInfo(
        string? shortName = null,
        ushort? union = null)
    {
        return null;
    }

    public override string FormatDisplay(T value, string? format = null)
        => value == null ? stringNull : TypeInfo.GetDescending(value.GetType())!.FormatDisplay(value, format);

    public override T FromString(ReadOnlySpan<char> value)
        => throw new NotSupportedException();

    public override string ToString(T value)
        => throw new NotImplementedException();

    public override string FormatQuery(T value, string? format = null)
        => value == null ? stringNull : TypeInfo.GetDescending(value.GetType())!.FormatDisplay(value, format);

    public override T Parse(object value)
    {
        throw new NotImplementedException();
    }

    public override void Read(XmlInvokeReader reader, ref T? value)
    {
        string? shortName = null;
        while (reader.MoveToNextAttribute())
        {
            if (reader.Name == stringXmlNil)
            {
                if (reader.ReadContentAsBoolean())
                {
                    value = default;
                    reader.MoveToElement();
                    return;
                }
            }
            if (reader.Name == stringXmlType)
            {
                shortName = reader.ReadContentAsString();
                reader.MoveToElement();
                break;
            }
        }
        var serializer = (shortName != null ? TypeInfo.GetDescending(shortName) : null)
            ?? FallbackTypeInfo(shortName: shortName)
            ?? throw new Exception($"Type Name {shortName ?? "NoName"} is not configured with union Type {typeof(T)}");
        var box = (object?)value;
        serializer.Read(reader, ref box);
        value = (T?)box;

    }

    public override void Write(XmlInvokeWriter writer, ref T? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        var serializer = TypeInfo.GetDescending(value.GetType())
            ?? throw new Exception($"Type {value.GetType()} is not configured with union Type {typeof(T)}");
        writer.WriteAttributeString(stringXmlType, serializer.TypeInfo.ShortName);
        var box = (object?)value;
        serializer.Write(writer, ref box);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref T? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            value = default;
            return;
        }
        string? shortName = null;
        if (reader.TokenType == JsonTokenType.Comment)
        {
            shortName = reader.GetComment()!;
            reader.Read();
        }
        var serializer = (shortName != null ? TypeInfo.GetDescending(shortName) : null)
            ?? FallbackTypeInfo(shortName: shortName)
            ?? throw new Exception($"Type Name {shortName ?? "NoName"} is not configured with union Type {typeof(T)}");
        var box = (object?)value;
        serializer!.Read(ref reader, ref box, options);
        value = (T?)box;
    }

    public override void Write(Utf8JsonWriter writer, ref T? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        var serializer = TypeInfo.GetDescending(value.GetType())
            ?? throw new Exception($"Type {value.GetType()} is not configured with union Type {typeof(T)}");
        writer.WriteCommentValue(serializer.TypeInfo.ShortName);
        var box = (object?)value;
        serializer.Write(writer, ref box, options);
    }

    public override void Read(ref MemoryPackReader reader, scoped ref T? value)
    {
        if (!reader.TryReadUnionHeader(out ushort union))
        {
            value = default;
            return;
        }
        var serialzier = TypeInfo.GetDescending(union)
            ?? FallbackTypeInfo(union: union)
            ?? throw new Exception($"Type Union {union} is not configured with union Type {typeof(T)}"); ;
        var box = (object?)value;
        serialzier.Read(ref reader, ref box);
        value = (T?)box;
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T? value)
    {
        if (value == null)
        {
            writer.WriteNullUnionHeader();
            return;
        }
        var serializer = TypeInfo.GetDescending(value.GetType())
            ?? throw new Exception($"Type {value.GetType()} is not configured with union Type {typeof(T)}");
        writer.WriteUnionHeader(serializer.TypeInfo.Union);
        var box = (object?)value;
        serializer.Write(ref writer, ref box);
    }

    public override void Read(ref MessagePackReader reader, scoped ref T? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = default;
            return;
        }

        var length = reader.ReadArrayHeader();
        if (length != 2)
            throw new MessagePackSerializationException($"Expected a two-item union for {typeof(T)}.");

        var union = reader.ReadUInt16();
        var serializer = TypeInfo.GetDescending(union)
            ?? FallbackTypeInfo(union: union)
            ?? throw new MessagePackSerializationException($"Type Union {union} is not configured with union Type {typeof(T)}");

        object? item = value;
        serializer.Read(ref reader, ref item, options);
        value = (T?)item;
    }

    public override void Write(ref MessagePackWriter writer, scoped ref T? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        var serializer = TypeInfo.GetDescending(value.GetType())
            ?? throw new MessagePackSerializationException($"Type {value.GetType()} is not configured with union Type {typeof(T)}");
        writer.WriteArrayHeader(2);
        writer.Write(serializer.TypeInfo.Union);
        object? item = value;
        serializer.Write(ref writer, ref item, options);
    }
}
