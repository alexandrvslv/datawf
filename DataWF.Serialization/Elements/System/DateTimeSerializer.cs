using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class DateTimeSerializer : PrimitiveSerializer<DateTime>
{
    public static readonly DateTimeSerializer Instance = new();

    public override DateTime FromString(ReadOnlySpan<char> value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
            ? result
            : DateTime.MinValue;

    public override string ToString(DateTime value) => value.ToString("o");

    public override string FormatDisplay(DateTime value, string? format = null) =>
        value.ToString(format, CultureInfo.InvariantCulture);

    public override string FormatQuery(DateTime value, string? format = null) =>
        $"'{value.ToString(format, CultureInfo.InvariantCulture).ToLower()}'";

    public override DateTime Parse(object value)
    {
        if (value is DateTime typedValue)
            return typedValue;
        if (value == null || value == DBNull.Value)
            return DateTime.MinValue;
        return Convert.ToDateTime(value, CultureInfo.InvariantCulture);
    }

    public override DateTime Read(DbDataReader reader, int i)
        => reader.GetDateTime(i);

    public override void Read(XmlInvokeReader reader, scoped ref DateTime value)
        => value = reader.ReadContentAsDateTime();

    public override void Write(XmlInvokeWriter writer, scoped ref DateTime value)
        => writer.WriteValue(value);

    public override void Read(ref Utf8JsonReader reader, scoped ref DateTime value, JsonSerializerOptions options)
        => value = reader.TryGetDateTime(out var rvalue) ? rvalue : default;

    public override void Write(Utf8JsonWriter writer, scoped ref DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);

    public override void Write(Utf8JsonWriter writer, scoped ref DateTime value, JsonSerializerOptions options,
        ISerializePropertyInfo propertyInfo)
        => writer.WriteString(propertyInfo.JsonName, value);

    public override void Read(ref MessagePackReader reader, scoped ref DateTime value, MessagePackSerializerOptions options)
        => value = MessagePack.Formatters.NativeDateTimeFormatter.Instance.Deserialize(ref reader, options);// reader.ReadDateTime();

    public override void Write(ref MessagePackWriter writer, scoped ref DateTime value, MessagePackSerializerOptions options)
        => MessagePack.Formatters.NativeDateTimeFormatter.Instance.Serialize(ref writer, value, options); //writer.Write(value);
}