using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class DateOnlyNSerializer : UnmanagedNullableSerializer<DateOnly>
{
    private const string Format = "yyyy-MM-dd";
    public new static readonly DateOnlyNSerializer Instance = new();

    public override DateOnly? FromString(ReadOnlySpan<char> value)
        => DateOnly.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
          ? result : null;

    public override string ToString(DateOnly? value)
        => value?.ToString(Format, CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(DateOnly? value, string? format = null)
        => value?.ToString(format ?? Format, CultureInfo.InvariantCulture) ?? stringNull;

    public override string FormatQuery(DateOnly? value, string? format = null)
        => value is DateOnly notNullValue
            ? $"'{notNullValue.ToString(format ?? Format, CultureInfo.InvariantCulture).ToLower()}'"
            : stringNull;

    public override DateOnly? Parse(object value)
    {
        if (value is DateOnly typedValue)
            return typedValue;
        if (value == null || value == DBNull.Value)
            return null;
        return DateOnly.TryParseExact(value.ToString(), Format, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
            ? result : null;
    }

    public override DateOnly? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetFieldValue<DateOnly>(i);

    public override void Read(XmlInvokeReader reader, scoped ref DateOnly? value)
        => value = reader.ReadContentAsString() is not { } stringValue || stringValue == stringXmlNil ? null
            : FromString(stringValue);

    public override void Write(XmlInvokeWriter writer, scoped ref DateOnly? value)
    {
        if (value.HasValue)
            writer.WriteValue(ToString(value));
        else
            writer.WriteNullValue();
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref DateOnly? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            value = null;
        else
        {
            var val = value ?? default;
            DateOnlySerializer.Instance.Read(ref reader, ref val, options);
            value = val;
        }
    }

    public override void Write(Utf8JsonWriter writer, scoped ref DateOnly? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
        {
            var val = value.Value; 
            DateOnlySerializer.Instance.Write(writer, ref val, options);
        }
    }

    public override void Write(Utf8JsonWriter writer, scoped ref DateOnly? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value == null)
            writer.WriteNull(propertyInfo.JsonName);
        else
        {
            var val = value.Value; 
            DateOnlySerializer.Instance.Write(writer, ref val, options, propertyInfo);
        }
    }
}
