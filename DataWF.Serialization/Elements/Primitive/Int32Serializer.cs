using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class Int32Serializer : PrimitiveSerializer<int>
{
    public static readonly Int32Serializer Instance = new();

    public override bool IsAttributeSerializer => true;

    public override int Read(DbDataReader reader, int i) => reader.GetInt32(i);

    public override int FromString(ReadOnlySpan<char> value)
        => int.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0;

    public override string ToString(int value)
        => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatDisplay(int value, string? format = null)
        => value.ToString(format, CultureInfo.InvariantCulture);

    public override int Parse(object? value)
        => Convert.ToInt32(value);

    public override void Read(XmlInvokeReader reader, scoped ref int value)
        => value = reader.ReadContentAsInt();

    public override void Write(XmlInvokeWriter writer, scoped ref int value)
        => writer.WriteValue(value);

    public override void Read(ref Utf8JsonReader reader, scoped ref int value, JsonSerializerOptions options)
        => value = reader.TryGetInt32(out var rvalue) ? rvalue : 0;

    public override void Write(Utf8JsonWriter writer, scoped ref int value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);

    public override void Write(Utf8JsonWriter writer, scoped ref int value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
        => writer.WriteNumber(propertyInfo.JsonName, value);

    public override void Read(ref MessagePackReader reader, scoped ref int value, MessagePackSerializerOptions options)
        => value = reader.ReadInt32();

    public override void Write(ref MessagePackWriter writer, scoped ref int value, MessagePackSerializerOptions options)
        => writer.Write(value);
    
}

