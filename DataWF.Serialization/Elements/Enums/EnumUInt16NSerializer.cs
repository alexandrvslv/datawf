using System;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class EnumUInt16NSerializer<T> : UnmanagedNullableSerializer<T> where T : unmanaged
{
    private static T UnsafeCast(ushort value) => Unsafe.As<ushort, T>(ref value);
    private static ushort UnsafeCast(T value) => Unsafe.As<T, ushort>(ref value);

    public new static readonly EnumUInt16NSerializer<T> Instance = new();

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
            ushort ushortNumber => UnsafeCast(ushortNumber),
            short shortNumber => UnsafeCast((ushort)shortNumber),
            int intNumber => UnsafeCast((ushort)intNumber),
            long longNumber => UnsafeCast((ushort)longNumber),
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
            var value = (ushort)reader.GetInt16(i);
            return Unsafe.As<ushort, T>(ref value);
        }
    }

    public override void Read(XmlInvokeReader reader, ref T? value)
    {
        ushort? number = default;
        UInt16NSerializer.Instance.Read(reader, ref number);
        if (number != null)
        {
            var notNull = number.Value;
            value = Unsafe.As<ushort, T>(ref notNull);
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
            writer.WriteValue(Unsafe.As<T, ushort>(ref notNull));
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
            reader.TryGetUInt16(out var number);
            value = Unsafe.As<ushort, T>(ref number);
        }
    }

    public override void Write(Utf8JsonWriter writer, scoped ref T? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            var number = value.Value;
            writer.WriteNumberValue(Unsafe.As<T, ushort>(ref number));
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
            writer.WriteNumber(propertyInfo.JsonName, Unsafe.As<T, ushort>(ref number));
        }
        else
        {
            writer.WriteNull(propertyInfo.JsonName);
        }
    }
}
