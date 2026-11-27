using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class UInt64NSerializer : UnmanagedNullableSerializer<ulong>
{
    public new static readonly UInt64NSerializer Instance = new();

    public override ulong? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetFieldValue<ulong>(i);

    public override ulong? FromString(ReadOnlySpan<char> value)
        => ulong.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    public override string ToString(ulong? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(ulong? value, string? format = null)
        => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;

    public override ulong? Parse(object? value)
        => value == null ? null : Convert.ToUInt64(value);

    public override void Read(XmlInvokeReader reader, scoped ref ulong? value)
        => value = reader.ReadContentAsString() is not { } stringValue || stringValue == stringXmlNil ? null
            : FromString(stringValue);

    public override void Write(XmlInvokeWriter writer, scoped ref ulong? value)
    {
        if (value.HasValue)
            writer.WriteValue((decimal)value.Value);
        else
            writer.WriteValue(stringXmlNil);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref ulong? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.TryGetUInt64(out var rvalue) ? rvalue : null;

    public override void Write(Utf8JsonWriter writer, scoped ref ulong? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref ulong? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
            writer.WriteNumber(propertyInfo.JsonName, value.Value);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override void Read(ref MessagePackReader reader, scoped ref ulong? value, MessagePackSerializerOptions options)
        => value = reader.TryReadNil() ? null : reader.ReadUInt64();

    public override void Write(ref MessagePackWriter writer, scoped ref ulong? value, MessagePackSerializerOptions options)
    {
        if(value == null)
            writer.WriteNil();
        else
           writer.Write(value.Value);
    }
}
