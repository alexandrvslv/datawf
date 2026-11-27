using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class Int64Serializer : PrimitiveSerializer<long>
{
    public static readonly Int64Serializer Instance = new();

    public override bool IsAttributeSerializer => true;

    public override long Read(DbDataReader reader, int i)
        => reader.GetInt64(i);

    public override long FromString(ReadOnlySpan<char> value)
        => long.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0L;

    public override string ToString(long value)
        => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatDisplay(long value, string? format = null)
        => value.ToString(format, CultureInfo.InvariantCulture);

    public override long Parse(object? value)
        => Convert.ToInt64(value);

    public override void Read(XmlInvokeReader reader, scoped ref long value)
        => value = reader.ReadContentAsLong();

    public override void Write(XmlInvokeWriter writer, scoped ref long value)
        => writer.WriteValue(value);

    public override void Read(ref Utf8JsonReader reader, scoped ref long value, JsonSerializerOptions options)
        => value = reader.TryGetInt64(out var rvalue) ? rvalue : 0L;

    public override void Write(Utf8JsonWriter writer, scoped ref long value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);

    public override void Write(Utf8JsonWriter writer, scoped ref long value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
        => writer.WriteNumber(propertyInfo.JsonName, value);

    public override void Read(ref MessagePackReader reader, scoped ref long value, MessagePackSerializerOptions options)
    => value = reader.ReadInt64();

    public override void Write(ref MessagePackWriter writer, scoped ref long value, MessagePackSerializerOptions options)
        => writer.Write(value);

}

