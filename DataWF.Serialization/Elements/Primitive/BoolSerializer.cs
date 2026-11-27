using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class BoolSerializer : PrimitiveSerializer<bool>
{
    public static readonly BoolSerializer Instance = new();

    public override bool Read(DbDataReader reader, int i)
        => reader.GetBoolean(i);

    public override void Read(XmlInvokeReader reader, scoped ref bool value)
        => value = reader.ReadContentAsBoolean();

    public override void Write(XmlInvokeWriter writer, scoped ref bool value)
        => writer.WriteValue(value);

    public override void Write(XmlInvokeWriter writer, scoped ref bool value, ISerializePropertyInfo propertyInfo)
        => writer.WriteAttributeString(propertyInfo.Name, ToString(value));

    public override void Read(ref Utf8JsonReader reader, scoped ref bool value, JsonSerializerOptions options)
        => value = reader.GetBoolean();

    public override void Write(Utf8JsonWriter writer, ref bool value, JsonSerializerOptions options)
        => writer.WriteBooleanValue(value);

    public override void Write(Utf8JsonWriter writer, ref bool value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
        => writer.WriteBoolean(propertyInfo.JsonName, value);

    public override void Read(ref MessagePackReader reader, scoped ref bool value, MessagePackSerializerOptions options)
        => value = reader.ReadBoolean();

    public override void Write(ref MessagePackWriter writer, scoped ref bool value, MessagePackSerializerOptions options)
        => writer.Write(value);

    public override bool FromString(ReadOnlySpan<char> value)
        => bool.TryParse(value, out var result) && result;

    public override string ToString(bool value) => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatDisplay(bool value, string? format = null)
        => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatQuery(bool value, string? format = null)
        => value.ToString(CultureInfo.InvariantCulture).ToLower();

    public override bool Parse(object value)
    {
        if (value is bool typedValue)
            return typedValue;
        if (value == null || value == DBNull.Value)
            return false;
        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
    }
}

