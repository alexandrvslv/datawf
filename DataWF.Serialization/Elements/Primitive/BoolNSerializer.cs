using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using System.Xml;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class BoolNSerializer : UnmanagedNullableSerializer<bool>
{
    public new static readonly BoolNSerializer Instance = new();

    public override bool? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetBoolean(i);

    public override void Read(XmlInvokeReader reader, ref bool? value)
        => value = reader.ReadContentAsString() is not { } stringValue || stringValue == stringXmlNil ? null : XmlConvert.ToBoolean(stringValue);

    public override void Write(XmlInvokeWriter writer, ref bool? value)
    {
        if (value.HasValue)
            writer.WriteValue(value.Value);
        else
            writer.WriteValue(stringXmlNil);
    }

    public override void Write(XmlInvokeWriter writer, ref bool? value, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
            writer.WriteAttributeString(propertyInfo.Name, ToString(value));
        else
            writer.WriteAttributeString(propertyInfo.Name, stringXmlNil);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref bool? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.GetBoolean();

    public override void Write(Utf8JsonWriter writer, scoped ref bool? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteBooleanValue(value.Value);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref bool? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
            writer.WriteBoolean(propertyInfo.JsonName, value.Value);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override void Read(ref MessagePackReader reader, scoped ref bool? value, MessagePackSerializerOptions options)
        => value = reader.TryReadNil() ? default : reader.ReadBoolean();

    public override void Write(ref MessagePackWriter writer, scoped ref bool? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            writer.Write(value.Value);
        }
    }

    public override bool? FromString(ReadOnlySpan<char> value)
        => bool.TryParse(value, out var result) && result;

    public override string ToString(bool? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(bool? value, string? format = null)
        => value?.ToString(CultureInfo.InvariantCulture) ?? stringNull;

    public override string FormatQuery(bool? value, string? format = null)
        => value?.ToString(CultureInfo.InvariantCulture).ToLower() ?? stringNull;

    public override bool? Parse(object value)
    {
        if (value is bool typedValue)
            return typedValue;
        if (value == null || value == DBNull.Value)
            return null;
        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
    }
}

