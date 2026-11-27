using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class Int32NSerializer : UnmanagedNullableSerializer<int>
{
    public new static readonly Int32NSerializer Instance = new();

    public override int? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetInt32(i);

    public override int? FromString(ReadOnlySpan<char> value)
        => int.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    public override string ToString(int? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(int? value, string? format = null)
        => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;

    public override int? Parse(object? value)
        => value == null ? null : Convert.ToInt32(value);

    public override void Read(XmlInvokeReader reader, scoped ref int? value)
        => value = reader.ReadContentAsString() is not { } stringValue || stringValue == stringXmlNil ? null : FromString(stringValue);

    public override void Write(XmlInvokeWriter writer, scoped ref int? value)
    {
        if (value.HasValue)
            writer.WriteValue(value.Value);
        else
            writer.WriteValue(stringXmlNil);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref int? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.TryGetInt32(out var rvalue) ? rvalue : null;

    public override void Write(Utf8JsonWriter writer, scoped ref int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref int? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
            writer.WriteNumber(propertyInfo.JsonName, value.Value);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override void Read(ref MessagePackReader reader, scoped ref int? value, MessagePackSerializerOptions options)
        => value = reader.TryReadNil() ? null : reader.ReadInt32();

    public override void Write(ref MessagePackWriter writer, scoped ref int? value, MessagePackSerializerOptions options)
    {
        if(value == null)
            writer.WriteNil();
        else
           writer.Write(value.Value);
    }
}
