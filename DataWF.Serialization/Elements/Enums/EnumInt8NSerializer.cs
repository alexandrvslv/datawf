using System;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class EnumInt8NSerializer<T> : UnmanagedNullableSerializer<T> where T : unmanaged
{
    private static T UnsafeCast(sbyte value) => Unsafe.As<sbyte, T>(ref value);
    private static sbyte UnsafeCast(T value) => Unsafe.As<T, sbyte>(ref value);

    public new static readonly EnumInt8NSerializer<T> Instance = new();

    public override string FormatDisplay(T? value, string? format = null)
        => value?.ToString() ?? stringNull;

    public override string FormatQuery(T? value, string? format = null)
        => value == null ? stringNull : UnsafeCast(value.Value).ToString(format, CultureInfo.InvariantCulture);

    public override T? FromString(ReadOnlySpan<char> value)
        => Enum.TryParse<T>(value, out var result) ? result : null;

    public override string ToString(T? value)
        => value?.ToString() ?? string.Empty;

    public override T? Parse(object value)
    {
        return value switch
        {
            T same => same,
            sbyte sbyteNumber => UnsafeCast(sbyteNumber),
            byte byteNumber => UnsafeCast((sbyte)byteNumber),
            int intNumber => UnsafeCast((sbyte)intNumber),
            string str => FromString(str),
            null => null,
            _ => Enum.TryParse<T>(value.ToString(), out var result) ? result : null,
        };
    }

    public override T? Read(DbDataReader reader, int i)
    {
        if (reader.IsDBNull(i))
        {
            return default;
        }
        else
        {
            var value = (sbyte)reader.GetByte(i);
            return Unsafe.As<sbyte, T>(ref value);
        }
    }

    public override void Read(XmlInvokeReader reader, ref T? value)
    {
        sbyte? number = default;
        Int8NSerializer.Instance.Read(reader, ref number);
        if (number != null)
        {
            var notNull = number.Value;
            value = Unsafe.As<sbyte, T>(ref notNull);
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
            writer.WriteValue(Unsafe.As<T, sbyte>(ref notNull));
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
            reader.TryGetSByte(out var number);
            value = Unsafe.As<sbyte, T>(ref number);
        }
    }

    public override void Write(Utf8JsonWriter writer, scoped ref T? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            var number = value.Value;
            writer.WriteNumberValue(Unsafe.As<T, sbyte>(ref number));
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
            writer.WriteNumber(propertyInfo.JsonName, Unsafe.As<T, sbyte>(ref number));
        }
        else
        {
            writer.WriteNull(propertyInfo.JsonName);
        }
    }
}
