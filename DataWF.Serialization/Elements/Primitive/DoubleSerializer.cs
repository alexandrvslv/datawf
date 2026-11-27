using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class DoubleSerializer : PrimitiveSerializer<double>
{
    public static readonly DoubleSerializer Instance = new();

    public override double Read(DbDataReader reader, int i) => reader.GetDouble(i);

    public override double FromString(ReadOnlySpan<char> value)
        => double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0D;

    public override string ToString(double value)
        => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatDisplay(double value, string? format = null)
       => value.ToString(format, CultureInfo.InvariantCulture);

    public override string FormatQuery(double value, string? format = null)
       => value.ToString(format, CultureInfo.InvariantCulture);

    public override double Parse(object value)
        => Convert.ToDouble(value);

    public override void Read(XmlInvokeReader reader, scoped ref double value)
        => value = reader.ReadContentAsDouble();

    public override void Write(XmlInvokeWriter writer, scoped ref double value)
        => writer.WriteValue(value);

    public override void Read(ref Utf8JsonReader reader, scoped ref double value, JsonSerializerOptions options)
        => value = reader.TryGetDouble(out var rvalue) ? rvalue : 0D;

    public override void Write(Utf8JsonWriter writer, scoped ref double value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);

    public override void Write(Utf8JsonWriter writer, scoped ref double value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
        => writer.WriteNumber(propertyInfo.JsonName, value);

    public override void Read(ref MessagePackReader reader, scoped ref double value, MessagePackSerializerOptions options)
        => value = reader.ReadDouble();

    public override void Write(ref MessagePackWriter writer, scoped ref double value, MessagePackSerializerOptions options)
        => writer.Write(value);
}
