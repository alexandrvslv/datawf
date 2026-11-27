using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class Int64NSerializer : UnmanagedNullableSerializer<long>
{
    public new static readonly Int64NSerializer Instance = new();

    public override long? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetInt64(i);

    public override long? FromString(ReadOnlySpan<char> value)
        => long.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    public override string ToString(long? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(long? value, string? format = null)
        => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;

    public override long? Parse(object? value)
        => value == null ? null : Convert.ToInt64(value);

    public override void Read(XmlInvokeReader reader, scoped ref long? value)
        => value = reader.ReadContentAsString() is not { } stringValue || stringValue == stringXmlNil ? null : FromString(stringValue);

    public override void Write(XmlInvokeWriter writer, scoped ref long? value)
    {
        if (value.HasValue)
            writer.WriteValue(value.Value);
        else
            writer.WriteValue(stringXmlNil);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref long? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.TryGetInt64(out var rvalue) ? rvalue : null;

    public override void Write(Utf8JsonWriter writer, scoped ref long? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref long? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
            writer.WriteNumber(propertyInfo.JsonName, value.Value);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override void Read(ref MessagePackReader reader, scoped ref long? value, MessagePackSerializerOptions options)
        => value = reader.TryReadNil() ? null : reader.ReadInt64();

    public override void Write(ref MessagePackWriter writer, scoped ref long? value, MessagePackSerializerOptions options)
    {
        if(value == null)
            writer.WriteNil();
        else
           writer.Write(value.Value);
    }
}
