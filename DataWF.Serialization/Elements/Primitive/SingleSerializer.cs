using System;
using System.ComponentModel;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using DataWF.Invokers;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class SingleSerializer : PrimitiveSerializer<float>
{
    public static readonly SingleSerializer Instance = new();

    public override float Read(DbDataReader reader, int i)
        => reader.GetFloat(i);

    public override float FromString(ReadOnlySpan<char> value)
        => float.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0F;

    public override string ToString(float value)
        => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatDisplay(float value, string? format = null)
       => value.ToString(format, CultureInfo.InvariantCulture);

    public override string FormatQuery(float value, string? format = null)
       => value.ToString(format, CultureInfo.InvariantCulture);

    public override float Parse(object value)
        => Convert.ToSingle(value);

    public override void Read(XmlInvokeReader reader, scoped ref float value)
        => value = reader.ReadContentAsFloat();

    public override void Write(XmlInvokeWriter writer, scoped ref float value)
        => writer.WriteValue(value);

    public override void Read(ref Utf8JsonReader reader, scoped ref float value, JsonSerializerOptions options)
        => value = reader.TryGetSingle(out var rvalue) ? rvalue : 0F;

    public override void Write(Utf8JsonWriter writer, scoped ref float value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);

    public override void Write(Utf8JsonWriter writer, scoped ref float value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
        => writer.WriteNumber(propertyInfo.JsonName, value);

    public override void Read(ref MessagePackReader reader, scoped ref float value, MessagePackSerializerOptions options)
        => value = reader.ReadSingle();

    public override void Write(ref MessagePackWriter writer, scoped ref float value, MessagePackSerializerOptions options)
        => writer.Write(value);
}
