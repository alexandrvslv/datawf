using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class UInt32NSerializer : UnmanagedNullableSerializer<uint>
{
    public new static readonly UInt32NSerializer Instance = new();

    public override uint? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : (uint)reader.GetInt64(i);

    public override uint? FromString(ReadOnlySpan<char> value)
        => uint.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : (uint?)null;

    public override string ToString(uint? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(uint? value, string? format = null)
        => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;

    public override uint? Parse(object? value)
        => value == null ? null : Convert.ToUInt32(value);

    public override void Read(XmlInvokeReader reader, scoped ref uint? value)
        => value = reader.ReadContentAsString() is not {} stringValue || stringValue == stringXmlNil ? null
            : FromString(stringValue);

    public override void Write(XmlInvokeWriter writer, scoped ref uint? value)
    {
        if (value.HasValue)
            writer.WriteValue(value.Value);
        else
            writer.WriteValue(stringXmlNil);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref uint? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.TryGetUInt32(out var rvalue) ? rvalue : null;

    public override void Write(Utf8JsonWriter writer, scoped ref uint? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref uint? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
            writer.WriteNumber(propertyInfo.JsonName, value.Value);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override void Read(ref MessagePackReader reader, scoped ref uint? value, MessagePackSerializerOptions options)
        => value = reader.TryReadNil() ? null : reader.ReadUInt32();

    public override void Write(ref MessagePackWriter writer, scoped ref uint? value, MessagePackSerializerOptions options)
    {
        if(value == null)
            writer.WriteNil();
        else
           writer.Write(value.Value);
    }
}

