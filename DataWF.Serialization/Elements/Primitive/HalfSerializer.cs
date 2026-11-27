using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class HalfSerializer : PrimitiveSerializer<Half>
{
    public static readonly HalfSerializer Instance = new();

    public override Half Read(DbDataReader reader, int i)
        => reader.GetFieldValue<Half>(i);

    public override Half FromString(ReadOnlySpan<char> value)
        => Half.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : (Half)0;

    public override string ToString(Half value)
        => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatDisplay(Half value, string? format = null)
       => value.ToString(format, CultureInfo.InvariantCulture);

    public override string FormatQuery(Half value, string? format = null)
       => value.ToString(format, CultureInfo.InvariantCulture);

    public override Half Parse(object value)
        => value switch
        {
            Half h => h,
            float f => (Half)f,
            double d => (Half)d,
            decimal m => (Half)m,
            _ => (Half)Convert.ToSingle(value)
        };

    public override void Read(XmlInvokeReader reader, scoped ref Half value)
        => value = (Half)reader.ReadContentAsFloat();

    public override void Write(XmlInvokeWriter writer, scoped ref Half value)
        => writer.WriteValue((float)value);

    public override void Read(ref Utf8JsonReader reader, scoped ref Half value, JsonSerializerOptions options)
        => value = (Half)reader.GetSingle();

    public override void Write(Utf8JsonWriter writer, scoped ref Half value, JsonSerializerOptions options)
        => writer.WriteNumberValue((float)value);

    public override void Write(Utf8JsonWriter writer, scoped ref Half value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
        => writer.WriteNumber(propertyInfo.JsonName, (float)value);

    public override void Read(ref MessagePackReader reader, scoped ref Half value, MessagePackSerializerOptions options)
        => value = (Half)reader.ReadSingle();

    public override void Write(ref MessagePackWriter writer, scoped ref Half value, MessagePackSerializerOptions options)
        => writer.Write((float)value);
}
