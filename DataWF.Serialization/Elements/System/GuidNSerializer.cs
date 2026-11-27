using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class GuidNSerializer : UnmanagedNullableSerializer<Guid>
{
    public new static readonly GuidNSerializer Instance = new();

    public override Guid? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetGuid(i);

    public override void Read(XmlInvokeReader reader, scoped ref Guid? value)
        => value = reader.ReadContentAsString() is not { } stringValue || stringValue == stringXmlNil ? null
            : FromString(stringValue);

    public override void Write(XmlInvokeWriter writer, scoped ref Guid? value)
    {
        if (value is Guid notNullValue)
            writer.WriteValue(ToString(notNullValue));
        else
            writer.WriteNullValue();
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref Guid? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.TryGetGuid(out var gvalue) ? gvalue : null;

    public override void Write(Utf8JsonWriter writer, scoped ref Guid? value, JsonSerializerOptions options)
    {
        if (value is Guid notNullValue)
            writer.WriteStringValue(notNullValue);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref Guid? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value is Guid notNullValue)
            writer.WriteString(propertyInfo.JsonName, notNullValue);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override Guid? FromString(ReadOnlySpan<char> value)
        => Guid.TryParse(value, out var result) ? result : null;

    public override string ToString(Guid? value)
        => value?.ToString() ?? string.Empty;

    public override string FormatDisplay(Guid? value, string? format = null)
        => value?.ToString(format, CultureInfo.InvariantCulture) ?? stringNull;

    public override string FormatQuery(Guid? value, string? format = null)
        => value is Guid notNullValue
        ? $"'{notNullValue.ToString(format, CultureInfo.InvariantCulture)}'"
        : stringNull;


    public override Guid? Parse(object value)
    {
        return value switch
        {
            Guid typedValue => typedValue,
            null => null,
            byte[] byteArray => new Guid(byteArray),
            _ => Guid.TryParse(value.ToString(), out var parsed) ? parsed : null
        };
    }

}