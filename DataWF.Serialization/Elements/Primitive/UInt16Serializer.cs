using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class UInt16Serializer : PrimitiveSerializer<ushort>
{
    public static readonly UInt16Serializer Instance = new();

    public override bool IsAttributeSerializer => true;

    public override ushort Read(DbDataReader reader, int i)
        => (ushort)reader.GetInt32(i);

    public override ushort FromString(ReadOnlySpan<char> value)
        => ushort.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : (ushort)0;

    public override string ToString(ushort value)
        => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatDisplay(ushort value, string? format = null)
        => value.ToString(format, CultureInfo.InvariantCulture);

    public override ushort Parse(object? value)
        => Convert.ToUInt16(value);

    public override void Read(XmlInvokeReader reader, scoped ref ushort value)
        => value = (ushort)reader.ReadContentAsInt();

    public override void Write(XmlInvokeWriter writer, scoped ref ushort value)
        => writer.WriteValue(value);

    public override void Read(ref Utf8JsonReader reader, scoped ref ushort value, JsonSerializerOptions options)
        => value = reader.TryGetUInt16(out var rvalue) ? rvalue : default;

    public override void Write(Utf8JsonWriter writer, scoped ref ushort value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);

    public override void Write(Utf8JsonWriter writer, scoped ref ushort value, JsonSerializerOptions options,
        ISerializePropertyInfo propertyInfo)
        => writer.WriteNumber(propertyInfo.JsonName, value);

    public override void Read(ref MessagePackReader reader, scoped ref ushort value, MessagePackSerializerOptions options)
        => value = reader.ReadUInt16();

    public override void Write(ref MessagePackWriter writer, scoped ref ushort value, MessagePackSerializerOptions options)
        => writer.Write(value);
}