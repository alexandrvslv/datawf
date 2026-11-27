using System;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class EnumInt64NSerializer<T> : UnmanagedNullableSerializer<T> where T : unmanaged
{
    private static T UnsafeCast(long value) => Unsafe.As<long, T>(ref value);
    private static long UnsafeCast(T value) => Unsafe.As<T, long>(ref value);

    public new static readonly EnumInt64NSerializer<T> Instance = new();

    public override string FormatDisplay(T? value, string? format = null)
        => value?.ToString() ?? stringNull;

    public override string FormatQuery(T? value, string? format = null)
        => value == null ? stringNull : UnsafeCast(value.Value).ToString(format, CultureInfo.InvariantCulture);

    public override T? FromString(ReadOnlySpan<char> value)
        => Enum.TryParse<T>(value, out var result) ? result : null;

    public override string ToString(T? value)
        => value?.ToString() ?? string.Empty;

    public override T? Parse(object value)
        => value switch
        {
            T same => same,
            long longNumber => UnsafeCast(longNumber),
            sbyte sbyteNumber => UnsafeCast((long)sbyteNumber),
            byte byteNumber => UnsafeCast((long)byteNumber),
            short shortNumber => UnsafeCast((long)shortNumber),
            int intNumber => UnsafeCast((long)intNumber),
            string str => FromString(str),
            null => null,
            _ => Enum.TryParse<T>(value.ToString(), out var result) ? result : null,
        };

    public override T? Read(DbDataReader reader, int i)
    {
        if (reader.IsDBNull(i))
        {
            return default;
        }
        else
        {
            var value = reader.GetInt64(i);
            return Unsafe.As<long, T>(ref value);
        }
    }

    public override void Read(XmlInvokeReader reader, ref T? value)
    {
        long? number = default;
        Int64NSerializer.Instance.Read(reader, ref number);
        if (number != null)
        {
            var notNull = number.Value;
            value = Unsafe.As<long, T>(ref notNull);
        }
        else
        {
            value = default;
        }
    }

    public override void Write(XmlInvokeWriter writer, ref T? value)
    {
        if (value.HasValue)
        {
            var notNull = value.Value;
            writer.WriteValue(Unsafe.As<T, long>(ref notNull));
        }
        else
        {
            writer.WriteValue(stringXmlNil);
        }
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref T? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            value = null;
        else
        {
            reader.TryGetInt64(out var number);
            value = Unsafe.As<long, T>(ref number);
        }
    }

    public override void Write(Utf8JsonWriter writer, scoped ref T? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            var number = value.Value;
            writer.WriteNumberValue(Unsafe.As<T, long>(ref number));
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    public override void Write(Utf8JsonWriter writer, scoped ref T? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value.HasValue)
        {
            var number = value.Value;
            writer.WriteNumber(propertyInfo.JsonName, Unsafe.As<T, long>(ref number));
        }
        else
        {
            writer.WriteNull(propertyInfo.JsonName);
        }
    }
}
