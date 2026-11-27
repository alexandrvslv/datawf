using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class UInt8NSerializer : UnmanagedNullableSerializer<byte>
{
    public new static readonly UInt8NSerializer Instance = new();

    public override byte? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : (byte)reader.GetByte(i);

    public override byte? FromString(ReadOnlySpan<char> value)
        => byte.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    public override string ToString(byte? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(byte? value, string? format = null)
        => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;

    public override byte? Parse(object? value)
        => value == null ? null : Convert.ToByte(value);

    public override void Read(XmlInvokeReader reader, scoped ref byte? value)
        => value = reader.ReadContentAsString() is not {} stringValue || stringValue == stringXmlNil ? null
            : FromString(stringValue);

    public override void Write(XmlInvokeWriter writer, scoped ref byte? value)
    {
        if (value.HasValue)
            writer.WriteValue(value.Value);
        else
            writer.WriteNullValue();
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref byte? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.TryGetByte(out var rvalue) ? rvalue : null;

    public override void Write(Utf8JsonWriter writer, scoped ref byte? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref byte? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
            writer.WriteNumber(propertyInfo.JsonName, value.Value);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override void Read(ref MessagePackReader reader, scoped ref byte? value, MessagePackSerializerOptions options)
        => value = reader.TryReadNil() ? null : reader.ReadByte();

    public override void Write(ref MessagePackWriter writer, scoped ref byte? value, MessagePackSerializerOptions options)
    {
        if(value == null)
            writer.WriteNil();
        else
           writer.Write(value.Value);
    }
}