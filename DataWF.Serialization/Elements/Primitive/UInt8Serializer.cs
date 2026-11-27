using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class UInt8Serializer : PrimitiveSerializer<byte>
{
    public static readonly UInt8Serializer Instance = new();

    public override bool IsAttributeSerializer => true;

    public override byte Read(DbDataReader reader, int i) => reader.GetByte(i);

    public override byte FromString(ReadOnlySpan<char> value)
        => byte.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : (byte)0;

    public override string ToString(byte value)
        => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatDisplay(byte value, string? format = null)
        => value.ToString(format, CultureInfo.InvariantCulture);

    public override byte Parse(object? value)
        => Convert.ToByte(value);

    public override void Read(XmlInvokeReader reader, scoped ref byte value)
        => value = (byte)reader.ReadContentAsInt();

    public override void Write(XmlInvokeWriter writer, scoped ref byte value)
        => writer.WriteValue(value);

    public override void Read(ref Utf8JsonReader reader, scoped ref byte value, JsonSerializerOptions options)
        => value = reader.TryGetByte(out var rvalue) ? rvalue : default;

    public override void Write(Utf8JsonWriter writer, scoped ref byte value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);

    public override void Write(Utf8JsonWriter writer, scoped ref byte value, JsonSerializerOptions options,
        ISerializePropertyInfo propertyInfo)
        => writer.WriteNumber(propertyInfo.JsonName, value);

    public override void Read(ref MessagePackReader reader, scoped ref byte value, MessagePackSerializerOptions options)
        => value = reader.ReadByte();

    public override void Write(ref MessagePackWriter writer, scoped ref byte value, MessagePackSerializerOptions options)
        => writer.Write(value);
}