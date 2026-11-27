using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using System.Xml;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class DateTimeOffsetNSerializer : UnmanagedNullableSerializer<DateTimeOffset>
{
    public new static readonly DateTimeOffsetNSerializer Instance = new();

    public override DateTimeOffset? FromString(ReadOnlySpan<char> value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
        ? result : null;

    public override string ToString(DateTimeOffset? value)
        => value?.ToString("o") ?? string.Empty;

    public override string FormatDisplay(DateTimeOffset? value, string? format = null)
        => value?.ToString(format, CultureInfo.InvariantCulture) ?? stringNull;

    public override string FormatQuery(DateTimeOffset? value, string? format = null)
        => value is DateTimeOffset notNulValue
            ? $"'{notNulValue.ToString(format, CultureInfo.InvariantCulture).ToLower()}'"
            : stringNull;

    public override DateTimeOffset? Parse(object value)
    {
        if (value is DateTimeOffset typedValue)
            return typedValue;
        if (value == null || value == DBNull.Value)
            return null;
        return DateTimeOffset.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out typedValue)
            ? typedValue
            : null;
    }

    public override DateTimeOffset? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetFieldValue<DateTimeOffset>(i);

    public override void Read(XmlInvokeReader reader, scoped ref DateTimeOffset? value)
        => value = reader.ReadContentAsString() is not { } stringValue || stringValue == stringXmlNil ? null
            : XmlConvert.ToDateTimeOffset(stringValue);

    public override void Write(XmlInvokeWriter writer, scoped ref DateTimeOffset? value)
    {
        if (value is DateTimeOffset notNullValue)
            writer.WriteValue(notNullValue);
        else
            writer.WriteNullValue();
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref DateTimeOffset? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.TryGetDateTimeOffset(out var gvalue) ? gvalue : null;

    public override void Write(Utf8JsonWriter writer, scoped ref DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is DateTimeOffset notNullValue)
            writer.WriteStringValue(notNullValue);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref DateTimeOffset? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value is DateTimeOffset notNullValue)
            writer.WriteString(propertyInfo.JsonName, notNullValue);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

}
