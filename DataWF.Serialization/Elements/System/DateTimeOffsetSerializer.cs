using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class DateTimeOffsetSerializer : PrimitiveSerializer<DateTimeOffset>
{
    public static readonly DateTimeOffsetSerializer Instance = new();

    public override DateTimeOffset FromString(ReadOnlySpan<char> value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
            ? result
            : DateTimeOffset.MinValue;

    public override string ToString(DateTimeOffset value)
        => value.ToString("o");

    public override string FormatDisplay(DateTimeOffset value, string? format = null)
        => value.ToString(format, CultureInfo.InvariantCulture);

    public override string FormatQuery(DateTimeOffset value, string? format = null)
        => $"'{value.ToString(format, CultureInfo.InvariantCulture).ToLower()}'";

    public override DateTimeOffset Parse(object value)
    {
        if (value is DateTimeOffset typedValue)
            return typedValue;
        if (value == null || value == DBNull.Value)
            return DateTimeOffset.MinValue;
        return DateTimeOffset.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None,
            out typedValue)
            ? typedValue
            : DateTimeOffset.MinValue;
    }

    public override DateTimeOffset Read(DbDataReader reader, int i)
        => reader.GetFieldValue<DateTimeOffset>(i);

    public override void Read(XmlInvokeReader reader, scoped ref DateTimeOffset value)
        => value = reader.ReadContentAsDateTimeOffset();

    public override void Write(XmlInvokeWriter writer, scoped ref DateTimeOffset value)
        => writer.WriteValue(value);

    public override void Read(ref Utf8JsonReader reader, scoped ref DateTimeOffset value, JsonSerializerOptions options)
        => value = reader.TryGetDateTimeOffset(out var rvalue) ? rvalue : default;

    public override void Write(Utf8JsonWriter writer, scoped ref DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);

    public override void Write(Utf8JsonWriter writer, scoped ref DateTimeOffset value, JsonSerializerOptions options,
        ISerializePropertyInfo propertyInfo)
        => writer.WriteString(propertyInfo.JsonName, value);

    public override void Read(ref MessagePackReader reader, scoped ref DateTimeOffset value, MessagePackSerializerOptions options)
        => value = MessagePack.Formatters.DateTimeOffsetFormatter.Instance.Deserialize(ref reader, options);

    public override void Write(ref MessagePackWriter writer, scoped ref DateTimeOffset value, MessagePackSerializerOptions options)
        => MessagePack.Formatters.DateTimeOffsetFormatter.Instance.Serialize(ref writer, value, options);
}