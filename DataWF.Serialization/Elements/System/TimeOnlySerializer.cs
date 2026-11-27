using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class TimeOnlySerializer : PrimitiveSerializer<TimeOnly>
{
    private const string Format = "HH:mm:ss.fffffff";
    private const int FormatLength = 16;
    private const string FormatO = "o";
    
    public static readonly TimeOnlySerializer Instance = new();

    public override TimeOnly FromString(ReadOnlySpan<char> value)
        => TimeOnly.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None,
            out var result)
            ? result
            : TimeOnly.MinValue;

    public override string ToString(TimeOnly value)
        => value.ToString(Format, CultureInfo.InvariantCulture);

    public override string FormatDisplay(TimeOnly value, string? format = null)
        => value.ToString(format ?? Format, CultureInfo.InvariantCulture);

    public override string FormatQuery(TimeOnly value, string? format = null)
        => $"'{value.ToString(format ?? Format, CultureInfo.InvariantCulture).ToLower()}'";

    public override TimeOnly Parse(object value)
    {
        if (value is TimeOnly typedValue)
            return typedValue;
        if (value == null || value == DBNull.Value)
            return TimeOnly.MinValue;
        return TimeOnly.TryParseExact(value.ToString(), Format, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var result)
            ? result
            : TimeOnly.MinValue;
    }

    public override TimeOnly Read(DbDataReader reader, int i)
        => reader.GetFieldValue<TimeOnly>(i);

    public override void Read(XmlInvokeReader reader, scoped ref TimeOnly value)
        => value = FromString(reader.ReadContentAsString());

    public override void Write(XmlInvokeWriter writer, scoped ref TimeOnly value)
        => writer.WriteValue(ToString(value));

    public override void Read(ref Utf8JsonReader reader, scoped ref TimeOnly value, JsonSerializerOptions options)
    {
        scoped ReadOnlySpan<byte> buffer;
        if (!reader.HasValueSequence && !reader.ValueIsEscaped)
        {
            buffer = reader.ValueSpan;
        }
        else
        {
            Span<byte> stackSpan = stackalloc byte[Format.Length];
            int bytesWritten = reader.CopyString(stackSpan);
            buffer = stackSpan[..bytesWritten];
        }

        value = TryGetTimeOnly(buffer, out var rvalue) ? rvalue : TimeOnly.MinValue;
    }

    public override void Write(Utf8JsonWriter writer, scoped ref TimeOnly value, JsonSerializerOptions options)
    {
        Span<byte> buffer = stackalloc byte[FormatLength];
        if (value.TryFormat(buffer, out var written, FormatO, CultureInfo.InvariantCulture))
            writer.WriteStringValue(buffer[..written]);
        else
            writer.WriteStringValue(ToString(value));
    }

    public override void Write(Utf8JsonWriter writer, scoped ref TimeOnly value, JsonSerializerOptions options,
        ISerializePropertyInfo propertyInfo)
    {
        Span<byte> buffer = stackalloc byte[FormatLength];
        if (value.TryFormat(buffer, out var written, FormatO, CultureInfo.InvariantCulture))
            writer.WriteString(propertyInfo.JsonName, buffer[..written]);
        else
            writer.WriteString(propertyInfo.JsonName, ToString(value));
    }

    public static bool TryGetTimeOnly(ReadOnlySpan<byte> utf8Span, out TimeOnly result)
    {
        int length = utf8Span.Length;

        if (length >= 8 && utf8Span[2] == (byte)':' && utf8Span[5] == (byte)':')
        {
            int hour = (utf8Span[0] - '0') * 10 + (utf8Span[1] - '0');
            int minute = (utf8Span[3] - '0') * 10 + (utf8Span[4] - '0');
            int second = (utf8Span[6] - '0') * 10 + (utf8Span[7] - '0');

            long ticks = 0;

            if (length > 9 && utf8Span[8] == (byte)'.')
            {
                long fractionValue = 0;
                int fractionDigits = 0;

                // Считываем только идущие подряд цифры фракции (макс 7)
                for (int i = 9; i < length && fractionDigits < 7; i++)
                {
                    byte b = utf8Span[i];
                    if (b < (byte)'0' || b > (byte)'9') break; // Наткнулись на конец строки или Z/ offset

                    fractionValue = fractionValue * 10 + (b - '0');
                    fractionDigits++;
                }

                ticks = fractionValue * ScaleFactors[fractionDigits];
            }

            // 3. Создаем TimeOnly напрямую через тики базового времени + тики фракции
            long baseTicks = (hour * TimeSpan.TicksPerHour) +
                             (minute * TimeSpan.TicksPerMinute) +
                             (second * TimeSpan.TicksPerSecond);
            result = new TimeOnly(baseTicks + ticks);
            return true;
        }

        result = default;
        return false;
    }

    public override void Read(ref MessagePackReader reader, scoped ref TimeOnly value, MessagePackSerializerOptions options)
        => value = MessagePack.Formatters.TimeOnlyFormatter.Instance.Deserialize(ref reader, options);

    public override void Write(ref MessagePackWriter writer, scoped ref TimeOnly value, MessagePackSerializerOptions options)
        => MessagePack.Formatters.TimeOnlyFormatter.Instance.Serialize(ref writer, value, options);
}