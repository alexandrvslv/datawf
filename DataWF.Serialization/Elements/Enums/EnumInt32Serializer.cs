using System;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class EnumInt32Serializer<T> : PrimitiveSerializer<T> where T : unmanaged, Enum
{
    private static T UnsafeCast(int value) => Unsafe.As<int, T>(ref value);

    public static readonly EnumInt32Serializer<T> Instance = new();

    public override bool IsAttributeSerializer => true;

    public override T Read(DbDataReader reader, int i)
    {
        var value = reader.GetInt32(i);
        return Unsafe.As<int, T>(ref value);
    }

    public override T FromString(ReadOnlySpan<char> value)
        => Enum.TryParse<T>(value, out var result) ? result : default(T);

    public override string ToString(T value)
        => value.ToString() ?? string.Empty;


    public override string FormatDisplay(T value, string? format = null)
        => value.ToString() ?? string.Empty;

    public override string FormatQuery(T value, string? format = null)
    {
        var number = Unsafe.As<T, int>(ref value);
        return number.ToString(format, CultureInfo.InvariantCulture);
    }

    public override T Parse(object value)
        => value switch
        {
            T same => same,
            int intNumber => UnsafeCast(intNumber),
            byte byteNumber => UnsafeCast((int)byteNumber),
            short shortNumber => UnsafeCast((int)shortNumber),
            string str => FromString(str),
            null => default,
            _ => Enum.TryParse<T>(value.ToString(), out var result) ? result : default,
        };

    public override void Read(XmlInvokeReader reader, scoped ref T value)
    {
        var number = reader.ReadContentAsInt();
        value = Unsafe.As<int, T>(ref number);
    }

    public override void Write(XmlInvokeWriter writer, scoped ref T value)
    {
        writer.WriteValue(Unsafe.As<T, int>(ref value));
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref T value, JsonSerializerOptions options)
    {
        reader.TryGetInt32(out var number);
        value = Unsafe.As<int, T>(ref number);
    }

    public override void Write(Utf8JsonWriter writer, scoped ref T value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(Unsafe.As<T, int>(ref value));
    }

    public override void Read(ref MessagePackReader reader, scoped ref T value, MessagePackSerializerOptions options)
    {
        var number = reader.ReadInt32();
        value = Unsafe.As<int, T>(ref number);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref T value, MessagePackSerializerOptions options)
    {
        writer.Write(Unsafe.As<T, int>(ref value));
    }
}
