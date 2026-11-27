using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using System.Xml;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class DateTimeNSerializer : UnmanagedNullableSerializer<DateTime>
{
    public new static readonly DateTimeNSerializer Instance = new();

    public override DateTime? FromString(ReadOnlySpan<char> value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
            ? result : null;

    public override string ToString(DateTime? value)
        => value?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(DateTime? value, string? format = null)
        => value?.ToString(format, CultureInfo.InvariantCulture) ?? stringNull;

    public override string FormatQuery(DateTime? value, string? format = null)
        => value is DateTime notNullValue
        ? $"'{notNullValue.ToString(format, CultureInfo.InvariantCulture)}'"
        : stringNull;

    public override DateTime? Parse(object value)
    {
        if (value is DateTime typedValue)
            return typedValue;
        if (value == null || value == DBNull.Value)
            return null;
        return Convert.ToDateTime(value, CultureInfo.InvariantCulture);
    }

    public override DateTime? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetDateTime(i);

    public override void Read(XmlInvokeReader reader, scoped ref DateTime? value)
        => value = reader.ReadContentAsString() is not { } stringValue || stringValue == stringXmlNil ? null
            : XmlConvert.ToDateTime(stringValue, XmlDateTimeSerializationMode.Unspecified);

    public override void Write(XmlInvokeWriter writer, scoped ref DateTime? value)
    {
        if (value is DateTime notNullValue)
            writer.WriteValue(notNullValue);
        else
            writer.WriteNullValue();
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref DateTime? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.TryGetDateTime(out var gvalue) ? gvalue : null;

    public override void Write(Utf8JsonWriter writer, scoped ref DateTime? value, JsonSerializerOptions options)
    {
        if (value is DateTime notNullValue)
            writer.WriteStringValue(notNullValue);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref DateTime? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value is DateTime notNullValue)
            writer.WriteString(propertyInfo.JsonName, notNullValue);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }



}
