using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class DecimalNSerializer : UnmanagedNullableSerializer<decimal>
{
    public new static readonly DecimalNSerializer Instance = new();

    public override decimal? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetDecimal(i);

    public override decimal? FromString(ReadOnlySpan<char> value)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    public override string ToString(decimal? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(decimal? value, string? format = null)
       => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatQuery(decimal? value, string? format = null)
       => value?.ToString(format, CultureInfo.InvariantCulture) ?? stringNull;

    public override decimal? Parse(object value)
        => value == null ? null : Convert.ToDecimal(value);

    public override void Read(XmlInvokeReader reader, ref decimal? value)
        => value = reader.ReadContentAsString() is not {} stringValue || stringValue == stringXmlNil  ? null : FromString(stringValue);

    public override void Write(XmlInvokeWriter writer, ref decimal? value)
    {
        if (value is decimal notNullValue)
            writer.WriteValue(notNullValue);
        else
            writer.WriteValue(stringXmlNil);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref decimal? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.TryGetDecimal(out var rvalue) ? rvalue : null;

    public override void Write(Utf8JsonWriter writer, ref decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, ref decimal? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
            writer.WriteNumber(propertyInfo.JsonName, value.Value);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    
}
