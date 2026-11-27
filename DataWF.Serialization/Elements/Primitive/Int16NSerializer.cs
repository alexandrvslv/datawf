using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class Int16NSerializer : UnmanagedNullableSerializer<short>
{
    public new static readonly Int16NSerializer Instance = new();

    public override short? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetInt16(i);

    public override short? FromString(ReadOnlySpan<char> value)
        => short.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    public override string ToString(short? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(short? value, string? format = null)
        => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;

    public override short? Parse(object? value)
        => value == null ? null : Convert.ToInt16(value);

    public override void Read(XmlInvokeReader reader, scoped ref short? value)
        => value = reader.ReadContentAsString() is not { } stringValue || stringValue == stringXmlNil ? null : FromString(stringValue);

    public override void Write(XmlInvokeWriter writer, scoped ref short? value)
    {
        if (value.HasValue)
            writer.WriteValue(value.Value);
        else
            writer.WriteValue(stringXmlNil);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref short? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.TryGetInt16(out var rvalue) ? rvalue : null;

    public override void Write(Utf8JsonWriter writer, scoped ref short? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref short? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
            writer.WriteNumber(propertyInfo.JsonName, value.Value);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override void Read(ref MessagePackReader reader, scoped ref short? value, MessagePackSerializerOptions options)
        => value = reader.TryReadNil() ? null : reader.ReadInt16();

    public override void Write(ref MessagePackWriter writer, scoped ref short? value, MessagePackSerializerOptions options)
    {
        if(value == null)
            writer.WriteNil();
        else
           writer.Write(value.Value);
    }
}
