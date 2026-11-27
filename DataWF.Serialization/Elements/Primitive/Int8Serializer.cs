using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class Int8Serializer : PrimitiveSerializer<sbyte>
{
    public static readonly Int8Serializer Instance = new();

    public override bool IsAttributeSerializer => true;

    public override sbyte Read(DbDataReader reader, int i)
        => (sbyte)reader.GetByte(i);

    public override sbyte FromString(ReadOnlySpan<char> value)
        => sbyte.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : (sbyte)0;

    public override string ToString(sbyte value)
        => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatDisplay(sbyte value, string? format = null)
        => value.ToString(format, CultureInfo.InvariantCulture);

    public override sbyte Parse(object? value)
        => Convert.ToSByte(value);

    public override void Read(XmlInvokeReader reader, scoped ref sbyte value)
        => value = (sbyte)reader.ReadContentAsInt();

    public override void Write(XmlInvokeWriter writer, scoped ref sbyte value)
        => writer.WriteValue(value);

    public override void Read(ref Utf8JsonReader reader, scoped ref sbyte value, JsonSerializerOptions options)
        => value = reader.TryGetSByte(out var rvalue) ? rvalue : default;

    public override void Write(Utf8JsonWriter writer, scoped ref sbyte value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);

    public override void Write(Utf8JsonWriter writer, scoped ref sbyte value, JsonSerializerOptions options,
        ISerializePropertyInfo propertyInfo)
        => writer.WriteNumber(propertyInfo.JsonName, value);

    public override void Read(ref MessagePackReader reader, scoped ref sbyte value, MessagePackSerializerOptions options)
        => value = reader.ReadSByte();

    public override void Write(ref MessagePackWriter writer, scoped ref sbyte value, MessagePackSerializerOptions options)
        => writer.Write(value);
}