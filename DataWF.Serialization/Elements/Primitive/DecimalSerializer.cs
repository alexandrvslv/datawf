using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class DecimalSerializer : PrimitiveSerializer<decimal>
{
    public static readonly DecimalSerializer Instance = new();

    public override decimal Read(DbDataReader reader, int i)
        => reader.GetDecimal(i);

    public override decimal FromString(ReadOnlySpan<char> value)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0M;

    public override string ToString(decimal value)
        => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatDisplay(decimal value, string? format = null)
       => value.ToString(format, CultureInfo.InvariantCulture);

    public override string FormatQuery(decimal value, string? format = null)
       => value.ToString(format, CultureInfo.InvariantCulture);

    public override decimal Parse(object value)
        => Convert.ToDecimal(value);

    public override void Read(XmlInvokeReader reader, ref decimal value)
        => value = reader.ReadContentAsDecimal();

    public override void Write(XmlInvokeWriter writer, ref decimal value)
        => writer.WriteValue(value);

    public override void Read(ref Utf8JsonReader reader, scoped ref decimal value, JsonSerializerOptions options)
        => value = reader.TryGetDecimal(out var rvalue) ? rvalue : 0M;

    public override void Write(Utf8JsonWriter writer, ref decimal value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);

    public override void Write(Utf8JsonWriter writer, ref decimal value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
        => writer.WriteNumber(propertyInfo.JsonName, value);

    public override void Read(ref MessagePackReader reader, scoped ref decimal value, MessagePackSerializerOptions options)
    {
        value = MessagePack.Formatters.DecimalFormatter.Instance.Deserialize(ref reader, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref decimal value, MessagePackSerializerOptions options)
    {
        MessagePack.Formatters.DecimalFormatter.Instance.Serialize(ref writer, value, options);
    }
}
