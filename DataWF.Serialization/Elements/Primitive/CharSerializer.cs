using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class CharSerializer : PrimitiveSerializer<char>
{
    public static readonly CharSerializer Instance = new();

    public override char Read(DbDataReader reader, int i)
        => reader.GetChar(i);

    public override void Read(XmlInvokeReader reader, ref char value)
        => value = (char)reader.ReadContentAsInt();

    public override void Write(XmlInvokeWriter writer, ref char value)
         => writer.WriteValue(value);

    public override void Read(ref Utf8JsonReader reader, scoped ref char value, JsonSerializerOptions options)
    {
        if (!reader.HasValueSequence)
        {
            var span = reader.ValueSpan;
            if (span.Length == 1)
            {
                value = (char)span[0];
                return;
            }
        }

        Span<char> buffer = stackalloc char[2]; 
        int charsRead = reader.CopyString(buffer);
        value = buffer[0];
    }

    public override void Write(Utf8JsonWriter writer, ref char value, JsonSerializerOptions options)
    {
        if (value <= 127)
        {
            ReadOnlySpan<byte> utf8Span = stackalloc byte[] { (byte)value };
            writer.WriteStringValue(utf8Span);
        }
        else
        {
            ReadOnlySpan<char> charSpan = stackalloc char[] { value };
            writer.WriteStringValue(charSpan);
        }
    }

    public override void Write(Utf8JsonWriter writer, ref char value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value <= 127)
        {
            ReadOnlySpan<byte> utf8Span = stackalloc byte[] { (byte)value };
            writer.WriteString(propertyInfo.JsonName, utf8Span);
        }
        else
        {
            ReadOnlySpan<char> charSpan = stackalloc char[] { value };
            writer.WriteString(propertyInfo.JsonName, charSpan);
        }
    }

    public override void Read(ref MessagePackReader reader, scoped ref char value, MessagePackSerializerOptions options)
        => value = reader.ReadChar();

    public override void Write(ref MessagePackWriter writer, scoped ref char value, MessagePackSerializerOptions options)
        => writer.Write(value);

    public override char FromString(ReadOnlySpan<char> value)
        => value.Length > 0 ? value[0] : default;

    public override string ToString(char value) => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatDisplay(char value, string? format = null) => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatQuery(char value, string? format = null) => value.ToString(CultureInfo.InvariantCulture).ToLower();

    public override char Parse(object value)
    {
        if (value is char typedValue)
            return typedValue;
        return Convert.ToChar(value, CultureInfo.InvariantCulture);
    }
}

