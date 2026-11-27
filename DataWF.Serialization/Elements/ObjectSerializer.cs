using System;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public class ObjectSerializer : ElementSerializer<object?>
{
    public static readonly ObjectSerializer Instance = new();

    public static new ISerializeTypeInfo<object?> GetTypeInfo()
        => new SerializeObjectInfo();

    public ObjectSerializer() : base(GetTypeInfo())
    { }

    public override bool IsAttributeSerializer => false;

    protected virtual IElementSerializer<object?>? FallbackTypeInfo(
        string? shortName = null,
        ushort? union = null)
    {
        return null;
    }

    public override string FormatDisplay(object? value, string? format = null)
        => value == null
            ? stringNull
            : TypeInfo.GetDescending(value.GetType())!.FormatDisplay(value, format);

    public override object FromString(ReadOnlySpan<char> value)
        => throw new NotSupportedException();

    public override string ToString(object? value)
        => throw new NotImplementedException();

    public override string FormatQuery(object? value, string? format = null)
        => value == null
            ? stringNull
            : TypeInfo.GetDescending(value.GetType())!.FormatDisplay(value, format);

    public override object Parse(object value)
        => throw new NotSupportedException();

    public override void Read(XmlInvokeReader reader, ref object? value)
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
                       ?? throw new Exception(
                           $"Type Name {shortName ?? "NoName"} is not configured with union {typeof(object)}");
        serializer.Read(reader, ref value);
    }

    public override void Write(XmlInvokeWriter writer, ref object? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var serializer = TypeInfo.GetDescending(value.GetType())
                       ?? throw new Exception(
                           $"Type {value.GetType()} is not configured with union {typeof(object)}");
        writer.WriteAttributeString(stringXmlType, serializer.TypeInfo.ShortName);
        serializer.Write(writer, ref value);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref object? value, JsonSerializerOptions options)
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

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                reader.Skip();
                reader.Read();
            }

            var propertyName = reader.GetString();
            reader.Read();
            if (propertyName == stringJsonType)
            {
                shortName = reader.GetString();
                reader.Read();
                reader.Read();
            }
        }

        var serializer = (shortName != null ? TypeInfo.GetDescending(shortName) : null)
                       ?? FallbackTypeInfo(shortName: shortName)
                       ?? throw new Exception(
                           $"Type Name {shortName ?? "NoName"} is not configured with union {typeof(object)}");
        serializer!.Read(ref reader, ref value, options);
        reader.Read();
    }

    public override void Write(Utf8JsonWriter writer, ref object? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var serializer = TypeInfo.GetDescending(value.GetType())
                       ?? throw new Exception(
                           $"Type {value.GetType()} is not configured with union  {typeof(object)}");
        writer.WriteStartObject();
        writer.WriteString(stringJsonType, serializer.TypeInfo.ShortName);
        writer.WritePropertyName(stringJsonValue);
        serializer.Write(writer, ref value, options);
        writer.WriteEndObject();
    }

    public override void Read(ref MemoryPackReader reader, scoped ref object? value)
    {
        if (!reader.TryReadUnionHeader(out ushort union))
        {
            value = default;
            return;
        }

        var serializer = TypeInfo.GetDescending(union)
                       ?? FallbackTypeInfo(union: union)
                       ?? throw new Exception(
                           $"Type Union {union} is not configured with union  {typeof(object)}");
        serializer.Read(ref reader, ref value);
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref object? value)
    {
        if (value == null)
        {
            writer.WriteNullUnionHeader();
            return;
        }

        var serializer = TypeInfo.GetDescending(value.GetType())
                       ?? throw new Exception(
                           $"Type {value.GetType()} is not configured with union  {typeof(object)}");
        writer.WriteUnionHeader(serializer.TypeInfo.Union);
        serializer.Write(ref writer, ref value);
    }

    public override void Read(ref MessagePackReader reader, scoped ref object? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = null;
            return;
        }

        var length = reader.ReadArrayHeader();
        if (length != 2)
            throw new MessagePackSerializationException("Expected a two-item object union.");

        var union = reader.ReadUInt16();
        var serializer = TypeInfo.GetDescending(union)
            ?? FallbackTypeInfo(union: union)
            ?? throw new MessagePackSerializationException($"Type Union {union} is not configured with union {typeof(object)}");
        serializer.Read(ref reader, ref value, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref object? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        var serializer = TypeInfo.GetDescending(value.GetType())
            ?? throw new MessagePackSerializationException($"Type {value.GetType()} is not configured with union {typeof(object)}");
        writer.WriteArrayHeader(2);
        writer.Write(serializer.TypeInfo.Union);
        serializer.Write(ref writer, ref value, options);
    }
}