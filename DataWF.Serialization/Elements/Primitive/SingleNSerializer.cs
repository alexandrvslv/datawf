using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class SingleNSerializer : UnmanagedNullableSerializer<float>
{
    public new static readonly SingleNSerializer Instance = new();

    public override float? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetFloat(i);

    public override float? FromString(ReadOnlySpan<char> value)
        => float.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    public override string ToString(float? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(float? value, string? format = null)
       => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatQuery(float? value, string? format = null)
       => value?.ToString(format, CultureInfo.InvariantCulture) ?? stringNull;

    public override float? Parse(object value)
        => value == null ? null : Convert.ToSingle(value);

    public override void Read(XmlInvokeReader reader, scoped ref float? value)
        => value = reader.ReadContentAsString() is not { } stringValue ? null : FromString(stringValue);

    public override void Write(XmlInvokeWriter writer, scoped ref float? value)
    {
        if (value is float notNullValue)
            writer.WriteValue(notNullValue);
        else
            writer.WriteValue(stringXmlNil);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref float? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.TryGetSingle(out var rvalue) ? rvalue : null;

    public override void Write(Utf8JsonWriter writer, scoped ref float? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref float? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
            writer.WriteNumber(propertyInfo.JsonName, value.Value);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override void Read(ref MessagePackReader reader, scoped ref float? value, MessagePackSerializerOptions options)
        => value = reader.TryReadNil() ? null : reader.ReadSingle();

    public override void Write(ref MessagePackWriter writer, scoped ref float? value, MessagePackSerializerOptions options)
    {
        if(value == null)
            writer.WriteNil();
        else
           writer.Write(value.Value);
    }
}
