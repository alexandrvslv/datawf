using System;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class EnumInt16Serializer<T> : PrimitiveSerializer<T> where T : unmanaged, Enum
{
    private static T UnsafeCast(short value) => Unsafe.As<short, T>(ref value);
    public static readonly EnumInt16Serializer<T> Instance = new();

    public override bool IsAttributeSerializer => true;
    
    public override T Read(DbDataReader reader, int i)
    {
        var value = reader.GetInt16(i);
        return Unsafe.As<short, T>(ref value);
    }

    public override T FromString(ReadOnlySpan<char> value)
        => Enum.TryParse<T>(value, out var result) ? result : default(T);

    public override string ToString(T value)
        => value.ToString() ?? string.Empty;


    public override string FormatDisplay(T value, string? format = null)
        => value.ToString() ?? string.Empty;

    public override string FormatQuery(T value, string? format = null)
    {
        var number = Unsafe.As<T, short>(ref value);
        return number.ToString(format, CultureInfo.InvariantCulture);
    }

    public override T Parse(object value)
        => value switch
        {
            T same => same,
            short shortNumber => UnsafeCast(shortNumber),
            byte byteNumber => UnsafeCast((short)byteNumber),
            int intNumber => UnsafeCast((short)intNumber),
            string str => FromString(str),
            null => default,
            _ => Enum.TryParse<T>(value.ToString(), out var result) ? result : default,
        };

    public override void Read(XmlInvokeReader reader, scoped ref T value)
    {
        var number = (short)reader.ReadContentAsInt();
        value = Unsafe.As<short, T>(ref number);
    }

    public override void Write(XmlInvokeWriter writer, scoped ref T value)
    {
        writer.WriteValue(Unsafe.As<T, short>(ref value));
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref T value, JsonSerializerOptions options)
    {
        reader.TryGetInt16(out var number);
        value = Unsafe.As<short, T>(ref number);
    }

    public override void Write(Utf8JsonWriter writer, scoped ref T value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(Unsafe.As<T, short>(ref value));
    }

    public override void Read(ref MessagePackReader reader, scoped ref T value, MessagePackSerializerOptions options)
    {
        var number = reader.ReadInt16();
        value = Unsafe.As<short, T>(ref number);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref T value, MessagePackSerializerOptions options)
    {
        writer.Write(Unsafe.As<T, short>(ref value));
    }
}
