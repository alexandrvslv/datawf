using System;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class EnumUInt32NSerializer<T> : UnmanagedNullableSerializer<T> where T : unmanaged
{
    private static T UnsafeCast(uint value) => Unsafe.As<uint, T>(ref value);
    private static uint UnsafeCast(T value) => Unsafe.As<T, uint>(ref value);

    public new static readonly EnumUInt32NSerializer<T> Instance = new();

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
            uint uintNumber => UnsafeCast(uintNumber),
            int intNumber => UnsafeCast((uint)intNumber),
            long longNumber => UnsafeCast((uint)longNumber),
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
            var value = (uint)reader.GetInt64(i);
            return Unsafe.As<uint, T>(ref value);
        }
    }

    public override void Read(XmlInvokeReader reader, ref T? value)
    {
        uint? number = default;
        UInt32NSerializer.Instance.Read(reader, ref number);
        if (number != null)
        {
            var notNull = number.Value;
            value = Unsafe.As<uint, T>(ref notNull);
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
            writer.WriteValue(Unsafe.As<T, uint>(ref notNull));
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
            reader.TryGetUInt32(out var number);
            value = Unsafe.As<uint, T>(ref number);
        }
    }

    public override void Write(Utf8JsonWriter writer, scoped ref T? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            var number = value.Value;
            writer.WriteNumberValue(Unsafe.As<T, uint>(ref number));
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
            writer.WriteNumber(propertyInfo.JsonName, Unsafe.As<T, uint>(ref number));
        }
        else
        {
            writer.WriteNull(propertyInfo.JsonName);
        }
    }
}
