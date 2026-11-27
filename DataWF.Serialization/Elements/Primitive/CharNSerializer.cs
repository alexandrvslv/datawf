using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class CharNSerializer : UnmanagedNullableSerializer<char>
{
    public new static readonly CharNSerializer Instance = new();

    public override char? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetChar(i);

    public override void Read(XmlInvokeReader reader, ref char? value)
        => value = reader.ReadContentAsString() is not { } stringValue || stringValue == stringXmlNil ? null : stringValue[0];

    public override void Write(XmlInvokeWriter writer, ref char? value)
    {
        if (value is char notNullValue)
            writer.WriteCharEntity(notNullValue);
        else
            writer.WriteValue(stringXmlNil);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref char? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            return;
        }
        Span<char> buffer = stackalloc char[2];
        int charsRead = reader.CopyString(buffer);
        value = buffer[0];
    }

    public override void Write(Utf8JsonWriter writer, ref char? value, JsonSerializerOptions options)
    {
        if (value is char notNullValue)
        {
            ReadOnlySpan<char> charSpan = stackalloc char[] { notNullValue };
            writer.WriteStringValue(charSpan);
        }
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, ref char? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value is char notNullValue)
        {
            ReadOnlySpan<char> charSpan = stackalloc char[] { notNullValue };
            writer.WriteString(propertyInfo.JsonName, charSpan);
        }
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override void Read(ref MessagePackReader reader, scoped ref char? value, MessagePackSerializerOptions options)
        => value = reader.TryReadNil() ? null : reader.ReadChar();

    public override void Write(ref MessagePackWriter writer, scoped ref char? value, MessagePackSerializerOptions options)
    {
        if (value == null)
            writer.WriteNil();
        else
            writer.Write(value.Value);
    }

    public override char? FromString(ReadOnlySpan<char> value)
        => value.Length > 0 ? value[0] : null;

    public override string ToString(char? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public override string FormatDisplay(char? value, string? format = null)
        => value?.ToString(CultureInfo.InvariantCulture) ?? stringNull;

    public override string FormatQuery(char? value, string? format = null)
        => value?.ToString(CultureInfo.InvariantCulture).ToLower() ?? stringNull;

    public override char? Parse(object value)
    {
        if (value is char typedValue)
            return typedValue;
        if (value == null || value == DBNull.Value)
            return null;
        return Convert.ToChar(value, CultureInfo.InvariantCulture);
    }
}

