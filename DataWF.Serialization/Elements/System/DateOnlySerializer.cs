using System;
using System.Buffers;
using System.Data.Common;
using System.Globalization;
using System.Runtime.Serialization.Json;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class DateOnlySerializer : PrimitiveSerializer<DateOnly>
{
    private const string Format = "yyyy-MM-dd";
    private const string FormatO = "O";
    private const int FormatLength = 10;
    public static readonly DateOnlySerializer Instance = new();

    public override DateOnly FromString(ReadOnlySpan<char> value)
        => DateOnly.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None,
            out var result)
            ? result
            : DateOnly.MinValue;

    public override string ToString(DateOnly value)
        => value.ToString(Format, CultureInfo.InvariantCulture);

    public override string FormatDisplay(DateOnly value, string? format = null)
        => value.ToString(format ?? Format, CultureInfo.InvariantCulture);

    public override string FormatQuery(DateOnly value, string? format = null)
        => $"'{value.ToString(format ?? Format, CultureInfo.InvariantCulture).ToLower()}'";

    public override DateOnly Parse(object value)
    {
        if (value is DateOnly typedValue)
            return typedValue;
        if (value == null || value == DBNull.Value)
            return DateOnly.MinValue;
        return DateOnly.TryParseExact(value.ToString(), Format, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var result)
            ? result
            : DateOnly.MinValue;
    }

    public override DateOnly Read(DbDataReader reader, int i)
        => reader.GetFieldValue<DateOnly>(i);

    public override void Read(XmlInvokeReader reader, scoped ref DateOnly value)
        => value = FromString(reader.ReadContentAsString());

    public override void Write(XmlInvokeWriter writer, scoped ref DateOnly value)
        => writer.WriteValue(ToString(value));

    public override void Read(ref Utf8JsonReader reader, scoped ref DateOnly value, JsonSerializerOptions options)
    {
        scoped ReadOnlySpan<byte> buffer;
        if (!reader.HasValueSequence && !reader.ValueIsEscaped)
        {
            buffer = reader.ValueSpan;
        }
        else
        {
            Span<byte> stackSpan = stackalloc byte[10];
            int bytesWritten = reader.CopyString(stackSpan);
            buffer = stackSpan[..bytesWritten];
        }

        value = TryGetDateOnly(buffer, out var rvalue) ? rvalue : DateOnly.MinValue;
    }

    public override void Write(Utf8JsonWriter writer, scoped ref DateOnly value, JsonSerializerOptions options)
    {
        Span<byte> buffer = stackalloc byte[FormatLength];
        if (value.TryFormat(buffer, out _, FormatO, CultureInfo.InvariantCulture))
        {
            writer.WriteStringValue(buffer);
        }
        else
        {
            writer.WriteStringValue(ToString(value));
        }
    }

    public override void Write(Utf8JsonWriter writer, scoped ref DateOnly value, JsonSerializerOptions options,
        ISerializePropertyInfo propertyInfo)
    {
        Span<byte> buffer = stackalloc byte[FormatLength];
        if (value.TryFormat(buffer, out _, FormatO, CultureInfo.InvariantCulture))
        {
            writer.WriteString(propertyInfo.JsonName, buffer);
        }
        else
        {
            writer.WriteString(propertyInfo.JsonName, ToString(value));
        }
    }

    public static bool TryGetDateOnly(ReadOnlySpan<byte> utf8Span, out DateOnly result)
    {
        if (utf8Span.Length == 10 && utf8Span[4] == (byte)'-' && utf8Span[7] == (byte)'-')
        {
            // Быстрая конвертация UTF-8 байт в числа (код символа '0' равен 48)
            int year = (utf8Span[0] - '0') * 1000 + (utf8Span[1] - '0') * 100 + (utf8Span[2] - '0') * 10 +
                       (utf8Span[3] - '0');
            int month = (utf8Span[5] - '0') * 10 + (utf8Span[6] - '0');
            int day = (utf8Span[8] - '0') * 10 + (utf8Span[9] - '0');

            result = new DateOnly(year, month, day);
            return true;
        }

        result = DateOnly.MinValue;
        return false;
    }

    public override void Read(ref MessagePackReader reader, scoped ref DateOnly value,
        MessagePackSerializerOptions options)
        => value = MessagePack.Formatters.DateOnlyFormatter.Instance.Deserialize(ref reader, options);

    public override void Write(ref MessagePackWriter writer, scoped ref DateOnly value,
        MessagePackSerializerOptions options)
        => MessagePack.Formatters.DateOnlyFormatter.Instance.Serialize(ref writer, value, options);
}