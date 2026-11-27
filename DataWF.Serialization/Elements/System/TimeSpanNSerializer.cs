using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class TimeSpanNSerializer : UnmanagedNullableSerializer<TimeSpan>
{
    public new static readonly TimeSpanNSerializer Instance = new();

    public override TimeSpan? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetFieldValue<TimeSpan>(i);

    public override void Read(XmlInvokeReader reader, scoped ref TimeSpan? value)
        => value = reader.ReadContentAsString() is not { } stringValue || stringValue == stringXmlNil ? null
            : FromString(stringValue);

    public override void Write(XmlInvokeWriter writer, scoped ref TimeSpan? value)
    {
        if (value is TimeSpan notNullValue)
            writer.WriteValue(ToString(notNullValue));
        else
            writer.WriteNullValue();
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref TimeSpan? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            value = null;
        else
        {
            var val = value ?? TimeSpan.Zero;
            TimeSpanSerializer.Instance.Read(ref reader, ref val, options);
            value = val;
        }
    }

    public override void Write(Utf8JsonWriter writer, scoped ref TimeSpan? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
        {
            var val = value.Value; 
            TimeSpanSerializer.Instance.Write(writer, ref val, options);
        }
    }

    public override void Write(Utf8JsonWriter writer, scoped ref TimeSpan? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value == null)
            writer.WriteNull(propertyInfo.JsonName);
        else
        {
            var val = value.Value; 
            TimeSpanSerializer.Instance.Write(writer, ref val, options, propertyInfo);
        }
    }

    public override TimeSpan? FromString(ReadOnlySpan<char> value)
        => TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var result) ? result : null;

    public override string ToString(TimeSpan? value)
        => value?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(TimeSpan? value, string? format = null)
        => value?.ToString(format, CultureInfo.InvariantCulture) ?? stringNull;

    public override string FormatQuery(TimeSpan? value, string? format = null)
        => value is TimeSpan notNullValue
        ? $"'{notNullValue.ToString(format, CultureInfo.InvariantCulture)}'"
        : stringNull;

    public override TimeSpan? Parse(object value)
    {
        if (value is TimeSpan typedValue)
            return typedValue;
        if (value == null || value == DBNull.Value)
            return null;
        return TimeSpan.TryParse(value.ToString(), CultureInfo.InvariantCulture, out var result)
            ? result : null;
    }

}
