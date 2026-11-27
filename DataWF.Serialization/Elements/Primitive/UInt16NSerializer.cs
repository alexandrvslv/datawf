using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class UInt16NSerializer : UnmanagedNullableSerializer<ushort>
{
    public new static readonly UInt16NSerializer Instance = new();

    public override ushort? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetFieldValue<ushort>(i);

    public override ushort? FromString(ReadOnlySpan<char> value)
        => ushort.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    public override string ToString(ushort? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(ushort? value, string? format = null)
        => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;

    public override ushort? Parse(object? value)
        => value == null ? null : Convert.ToUInt16(value);

    public override void Read(XmlInvokeReader reader, scoped ref ushort? value)
        => value = reader.ReadContentAsString() is not { } stringValue || stringValue == stringXmlNil ? null
            : FromString(stringValue);

    public override void Write(XmlInvokeWriter writer, scoped ref ushort? value)
    {
        if (value.HasValue)
            writer.WriteValue(value.Value);
        else
            writer.WriteValue(stringXmlNil);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref ushort? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.TryGetUInt16(out var rvalue) ? rvalue : null;

    public override void Write(Utf8JsonWriter writer, scoped ref ushort? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref ushort? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
            writer.WriteNumber(propertyInfo.JsonName, value.Value);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override void Read(ref MessagePackReader reader, scoped ref ushort? value, MessagePackSerializerOptions options)
        => value = reader.TryReadNil() ? null : reader.ReadUInt16();

    public override void Write(ref MessagePackWriter writer, scoped ref ushort? value, MessagePackSerializerOptions options)
    {
        if(value == null)
            writer.WriteNil();
        else
           writer.Write(value.Value);
    }
}
