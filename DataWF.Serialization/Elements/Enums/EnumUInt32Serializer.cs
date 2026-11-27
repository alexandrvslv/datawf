using System;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class EnumUInt32Serializer<T> : PrimitiveSerializer<T> where T : unmanaged
{
    private static T UnsafeCast(uint value) => Unsafe.As<uint, T>(ref value);

    public static readonly EnumUInt32Serializer<T> Instance = new();

    public override bool IsAttributeSerializer => true;

    public override T Read(DbDataReader reader, int i)
    {
        var value = (uint)reader.GetInt64(i);
        return Unsafe.As<uint, T>(ref value);
    }

    public override T FromString(ReadOnlySpan<char> value)
        => Enum.TryParse<T>(value, out var result) ? result : default(T);

    public override string ToString(T value)
        => value.ToString() ?? string.Empty;


    public override string FormatDisplay(T value, string? format = null)
        => value.ToString() ?? string.Empty;

    public override string FormatQuery(T value, string? format = null)
    {
        var number = Unsafe.As<T, uint>(ref value);
        return number.ToString(format, CultureInfo.InvariantCulture);
    }

    public override T Parse(object value)
        => value switch
        {
            T same => same,
            uint uintNumber => Unsafe.As<uint, T>(ref uintNumber),
            ushort ushortNumber => UnsafeCast((uint)ushortNumber),
            byte byteNumber => UnsafeCast((uint)byteNumber),
            sbyte sbyteNumber => UnsafeCast((uint)sbyteNumber),
            int intNumber => UnsafeCast((uint)intNumber),
            string str => FromString(str),
            null => default,
            _ => Enum.TryParse<T>(value.ToString(), out var result) ? result : default,
        };

    public override void Read(XmlInvokeReader reader, scoped ref T value)
    {
        var number = (uint)reader.ReadContentAsLong();
        value = Unsafe.As<uint, T>(ref number);
    }

    public override void Write(XmlInvokeWriter writer, scoped ref T value)
    {
        writer.WriteValue(Unsafe.As<T, uint>(ref value));
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref T value, JsonSerializerOptions options)
    {
        reader.TryGetUInt32(out var number);
        value = Unsafe.As<uint, T>(ref number);
    }

    public override void Write(Utf8JsonWriter writer, scoped ref T value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(Unsafe.As<T, uint>(ref value));
    }

    public override void Write(Utf8JsonWriter writer, scoped ref T value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        writer.WriteNumber(propertyInfo.JsonName, Unsafe.As<T, uint>(ref value));
    }

    public override void Read(ref MessagePackReader reader, scoped ref T value, MessagePackSerializerOptions options)
    {
        var number = reader.ReadUInt32();
        value = Unsafe.As<uint, T>(ref number);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref T value, MessagePackSerializerOptions options)
    {
        writer.Write(Unsafe.As<T, uint>(ref value));
    }
}
