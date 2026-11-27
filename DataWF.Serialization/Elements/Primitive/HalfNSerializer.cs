using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class HalfNSerializer : UnmanagedNullableSerializer<Half>
{
    public new static readonly HalfNSerializer Instance = new();

    public override Half? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetFieldValue<Half>(i);

    public override Half? FromString(ReadOnlySpan<char> value)
        => Half.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    public override string ToString(Half? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(Half? value, string? format = null)
       => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatQuery(Half? value, string? format = null)
       => value?.ToString(format, CultureInfo.InvariantCulture) ?? stringNull;

    public override Half? Parse(object value)
        => value == null ? null : (Half)Convert.ToSingle(value);

    public override void Read(XmlInvokeReader reader, scoped ref Half? value)
        => value = (Half)reader.ReadContentAsFloat();

    public override void Write(XmlInvokeWriter writer, scoped ref Half? value)
    {
        if (value is Half notNullValue)
            writer.WriteValue((float)notNullValue);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref Half? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : (Half)reader.GetSingle();

    public override void Write(Utf8JsonWriter writer, scoped ref Half? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue((float)value.Value);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref Half? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
            writer.WriteNumber(propertyInfo.JsonName, (float)value.Value);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override void Read(ref MessagePackReader reader, scoped ref Half? value, MessagePackSerializerOptions options)
        => value = reader.TryReadNil() ? null : (Half)reader.ReadSingle();

    public override void Write(ref MessagePackWriter writer, scoped ref Half? value, MessagePackSerializerOptions options)
    {
        if (value.HasValue)
            writer.Write((float)value);
        else
            writer.WriteNil();
    }
}
