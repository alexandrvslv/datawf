using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class DoubleNSerializer : UnmanagedNullableSerializer<double>
{
    public new static readonly DoubleNSerializer Instance = new();

    public override double? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetDouble(i);

    public override double? FromString(ReadOnlySpan<char> value)
        => double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    public override string ToString(double? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(double? value, string? format = null)
       => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatQuery(double? value, string? format = null)
       => value?.ToString(format, CultureInfo.InvariantCulture) ?? stringNull;

    public override double? Parse(object value)
        => value == null ? null : Convert.ToDouble(value);

    public override void Read(XmlInvokeReader reader, ref double? value)
        => value = reader.ReadContentAsDouble();

    public override void Write(XmlInvokeWriter writer, ref double? value)
    {
        if (value.HasValue)
            writer.WriteValue(value.Value);
    }

    public override void Write(XmlInvokeWriter writer, ref double? value, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
            writer.WriteAttributeString(propertyInfo.Name, ToString(value));
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref double? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.TryGetDouble(out var rvalue) ? rvalue : null;

    public override void Write(Utf8JsonWriter writer, ref double? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, ref double? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
            writer.WriteNumber(propertyInfo.JsonName, value.Value);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override void Read(ref MessagePackReader reader, scoped ref double? value, MessagePackSerializerOptions options)
        => value = reader.TryReadNil() ? null : reader.ReadDouble();

    public override void Write(ref MessagePackWriter writer, scoped ref double? value, MessagePackSerializerOptions options)
    {
        if(value == null)
            writer.WriteNil();
        else
           writer.Write(value.Value);
    }
}
