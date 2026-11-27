using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class Int16Serializer : PrimitiveSerializer<short>
{
    public static readonly Int16Serializer Instance = new();

    public override bool IsAttributeSerializer => true;

    public override short Read(DbDataReader reader, int i) => reader.GetInt16(i);

    public override short FromString(ReadOnlySpan<char> value)
        => short.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : (short)0;

    public override string ToString(short value)
        => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatDisplay(short value, string? format = null)
        => value.ToString(format, CultureInfo.InvariantCulture);

    public override short Parse(object? value)
        => Convert.ToInt16(value);

    public override void Read(XmlInvokeReader reader, scoped ref short value)
        => value = (short)reader.ReadContentAsInt();

    public override void Write(XmlInvokeWriter writer, scoped ref short value)
        => writer.WriteValue(value);

    public override void Read(ref Utf8JsonReader reader, scoped ref short value, JsonSerializerOptions options)
        => value = reader.TryGetInt16(out var rvalue) ? rvalue : default;

    public override void Write(Utf8JsonWriter writer, scoped ref short value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);

    public override void Write(Utf8JsonWriter writer, scoped ref short value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
        => writer.WriteNumber(propertyInfo.JsonName, value);

    public override void Read(ref MessagePackReader reader, scoped ref short value, MessagePackSerializerOptions options)
        => value = reader.ReadInt16();

    public override void Write(ref MessagePackWriter writer, scoped ref short value, MessagePackSerializerOptions options)
        => writer.Write(value);
    
}

