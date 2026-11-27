using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class TimeSpanSerializer : PrimitiveSerializer<TimeSpan>
{
    private const string Format = "c";

    public static readonly TimeSpanSerializer Instance = new();

    public override TimeSpan FromString(ReadOnlySpan<char> value)
        => TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var timeSpan) ? timeSpan : TimeSpan.Zero;

    public override string ToString(TimeSpan value)
        => value.ToString(Format, CultureInfo.InvariantCulture);

    public override string FormatDisplay(TimeSpan value, string? format = null)
        => value.ToString(format, CultureInfo.InvariantCulture);

    public override string FormatQuery(TimeSpan value, string? format = null)
        => $"'{value.ToString(format, CultureInfo.InvariantCulture).ToLower()}'";

    public override TimeSpan Parse(object value)
    {
        if (value is TimeSpan typedValue)
            return typedValue;
        if (value == null || value == DBNull.Value)
            return TimeSpan.Zero;
        return TimeSpan.TryParse(value.ToString(), CultureInfo.InvariantCulture, out var timeSpan)
            ? timeSpan
            : TimeSpan.Zero;
    }

    public override TimeSpan Read(DbDataReader reader, int i)
        => reader.GetFieldValue<TimeSpan>(i);

    public override void Read(XmlInvokeReader reader, scoped ref TimeSpan value)
        => value = FromString(reader.ReadContentAsString());

    public override void Write(XmlInvokeWriter writer, scoped ref TimeSpan value)
        => writer.WriteValue(ToString(value));

    public override void Read(ref Utf8JsonReader reader, scoped ref TimeSpan value, JsonSerializerOptions options)
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

        value = TryGetTimeSpan(buffer, out var rvalue) ? rvalue : TimeSpan.Zero;
    }

    public override void Write(Utf8JsonWriter writer, scoped ref TimeSpan value, JsonSerializerOptions options)
    {
        Span<byte> buffer = stackalloc byte[32];
        if (value.TryFormat(buffer, out var charsWritten, Format))
            writer.WriteStringValue(buffer[..charsWritten]);
        else
            writer.WriteStringValue(ToString(value));
    }

    public override void Write(Utf8JsonWriter writer, scoped ref TimeSpan value, JsonSerializerOptions options,
        ISerializePropertyInfo propertyInfo)
    {
        Span<byte> buffer = stackalloc byte[32];
        if (value.TryFormat(buffer, out var charsWritten, Format))
            writer.WriteString(propertyInfo.JsonName, buffer[..charsWritten]);
        else
            writer.WriteString(propertyInfo.JsonName, ToString(value));
    }

    public override void Read(ref MessagePackReader reader, scoped ref TimeSpan value, MessagePackSerializerOptions options)
        => value = MessagePack.Formatters.TimeSpanFormatter.Instance.Deserialize(ref reader, options);

    public override void Write(ref MessagePackWriter writer, scoped ref TimeSpan value, MessagePackSerializerOptions options)
        => MessagePack.Formatters.TimeSpanFormatter.Instance.Serialize(ref writer, value, options);

    public static bool TryGetTimeSpan(ReadOnlySpan<byte> utf8Span, out TimeSpan result)
    {
        result = TimeSpan.Zero;
        int length = utf8Span.Length;
        if (length == 0) return false;

        int index = 0;
        bool isNegative = false;

        if (utf8Span[0] == (byte)'-')
        {
            isNegative = true;
            index++;
        }

        int firstValue = 0;
        while (index < length && utf8Span[index] >= (byte)'0' && utf8Span[index] <= (byte)'9')
        {
            firstValue = firstValue * 10 + (utf8Span[index] - '0');
            index++;
        }

        long days = 0;
        int hours = 0;

        // "3.12:00:00"
        if (index < length && utf8Span[index] == (byte)'.')
        {
            days = firstValue;
            index++;

            while (index < length && utf8Span[index] >= (byte)'0' && utf8Span[index] <= (byte)'9')
            {
                hours = hours * 10 + (utf8Span[index] - '0');
                index++;
            }
        }
        else
        {
            // "12:00:00"
            hours = firstValue;
        }

        if (index >= length || utf8Span[index] != (byte)':')
            return false;
        index++;

        int minutes = 0;
        if (index + 2 <= length)
        {
            minutes = (utf8Span[index] - '0') * 10 + (utf8Span[index + 1] - '0');
            index += 2;
        }

        if (index >= length || utf8Span[index] != (byte)':')
            return false;
        index++;

        int seconds = 0;
        if (index + 2 <= length)
        {
            seconds = (utf8Span[index] - '0') * 10 + (utf8Span[index + 1] - '0');
            index += 2;
        }

        long fractionTicks = 0;

        if (index < length && utf8Span[index] == (byte)'.')
        {
            index++;
            long fractionValue = 0;
            int fractionDigits = 0;

            while (index < length && fractionDigits < 7)
            {
                byte b = utf8Span[index];
                if (b < (byte)'0' || b > (byte)'9') break;

                fractionValue = fractionValue * 10 + (b - '0');
                fractionDigits++;
                index++;
            }

            // Быстрое выравнивание до 7 знаков через массив множителей
            fractionTicks = fractionValue * ScaleFactors[fractionDigits];
        }

        // 6. Считаем итоговые тики
        long totalTicks = (days * TimeSpan.TicksPerDay) +
                          (hours * TimeSpan.TicksPerHour) +
                          (minutes * TimeSpan.TicksPerMinute) +
                          (seconds * TimeSpan.TicksPerSecond) +
                          fractionTicks;

        if (isNegative) totalTicks = -totalTicks;

        result = new TimeSpan(totalTicks);
        return true;
    }

    
}