using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class TimeOnlyNSerializer : UnmanagedNullableSerializer<TimeOnly>
{
    private const string Format = "HH:mm:ss.FFFFFFF";
    public new static readonly TimeOnlyNSerializer Instance = new();

    public override TimeOnly? FromString(ReadOnlySpan<char> value)
        => TimeOnly.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
            ? result : null;

    public override string ToString(TimeOnly? value)
        => value?.ToString(Format, CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(TimeOnly? value, string? format = null)
        => value?.ToString(format ?? Format, CultureInfo.InvariantCulture) ?? stringNull;

    public override string FormatQuery(TimeOnly? value, string? format = null)
        => value is TimeOnly notNullValue
            ? $"'{notNullValue.ToString(format ?? Format, CultureInfo.InvariantCulture).ToLower()}'"
            : stringNull;

    public override TimeOnly? Parse(object value)
    {
        if (value is TimeOnly typedValue)
            return typedValue;
        if (value == null || value == DBNull.Value)
            return null;
        return TimeOnly.TryParseExact(value.ToString(), Format, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
            ? result : null;
    }

    public override TimeOnly? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetFieldValue<TimeOnly>(i);

    public override void Read(XmlInvokeReader reader, scoped ref TimeOnly? value)
        => value = reader.ReadContentAsString() is not {} stringValue || stringValue == stringXmlNil ? null
            : FromString(stringValue);

    public override void Write(XmlInvokeWriter writer, scoped ref TimeOnly? value)
    {
        if(value is TimeOnly notNullValue)
            writer.WriteValue(ToString(notNullValue));
        else
            writer.WriteNullValue();
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref TimeOnly? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            value = null;
        else
        {
            var val = value ?? default;
            TimeOnlySerializer.Instance.Read(ref reader, ref val, options);
            value = val;
        }
    }

    public override void Write(Utf8JsonWriter writer, scoped ref TimeOnly? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
        {
            var val = value.Value; 
            TimeOnlySerializer.Instance.Write(writer, ref val, options);
        }
    }

    public override void Write(Utf8JsonWriter writer, scoped ref TimeOnly? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value == null)
            writer.WriteNull(propertyInfo.JsonName);
        else
        {
            var val = value.Value; 
            TimeOnlySerializer.Instance.Write(writer, ref val, options, propertyInfo);
        }
    }
}