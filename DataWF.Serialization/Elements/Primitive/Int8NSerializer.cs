using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class Int8NSerializer : UnmanagedNullableSerializer<sbyte>
{
    public new static readonly Int8NSerializer Instance = new();

    public override sbyte? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : (sbyte)reader.GetInt16(i);

    public override sbyte? FromString(ReadOnlySpan<char> value)
        => sbyte.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    public override string ToString(sbyte? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(sbyte? value, string? format = null)
        => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;

    public override sbyte? Parse(object value)
        => value == null ? null : Convert.ToSByte(value);

    public override void Read(XmlInvokeReader reader, scoped ref sbyte? value)
        => value = reader.ReadContentAsString() is not { } stringValue || stringValue == stringXmlNil ? null
            : FromString(stringValue);

    public override void Write(XmlInvokeWriter writer, scoped ref sbyte? value)
    {
        if (value.HasValue)
            writer.WriteValue(value.Value);
        else
            writer.WriteValue(stringXmlNil);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref sbyte? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null
            : reader.TryGetSByte(out var rvalue) ? rvalue : null;

    public override void Write(Utf8JsonWriter writer, scoped ref sbyte? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref sbyte? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
            writer.WriteNumber(propertyInfo.JsonName, value.Value);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override void Read(ref MessagePackReader reader, scoped ref sbyte? value, MessagePackSerializerOptions options)
        => value = reader.TryReadNil() ? null : reader.ReadSByte();

    public override void Write(ref MessagePackWriter writer, scoped ref sbyte? value, MessagePackSerializerOptions options)
    {
        if(value == null)
            writer.WriteNil();
        else
           writer.Write(value.Value);
    }
}
