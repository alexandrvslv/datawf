using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class UInt64Serializer : PrimitiveSerializer<ulong>
{
    public static readonly UInt64Serializer Instance = new();
    
    public override bool IsAttributeSerializer => true;
    
    public override ulong Read(DbDataReader reader, int i)
        => (ulong)reader.GetInt64(i);

    public override ulong FromString(ReadOnlySpan<char> value)
        => ulong.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : (ulong)0;

    public override string ToString(ulong value)
        => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatDisplay(ulong value, string? format = null)
        => value.ToString(format, CultureInfo.InvariantCulture);

    public override ulong Parse(object? value)
        => Convert.ToUInt64(value);

    public override void Read(XmlInvokeReader reader, scoped ref ulong value)
        => value = (ulong)reader.ReadContentAsLong();

    public override void Write(XmlInvokeWriter writer, scoped ref ulong value)
        => writer.WriteValue((decimal)value);

    public override void Read(ref Utf8JsonReader reader, scoped ref ulong value, JsonSerializerOptions options)
        => value = reader.TryGetUInt64(out var rvalue) ? rvalue : 0L;

    public override void Write(Utf8JsonWriter writer, scoped ref ulong value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);

    public override void Write(Utf8JsonWriter writer, scoped ref ulong value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
        => writer.WriteNumber(propertyInfo.JsonName, value);

    public override void Read(ref MessagePackReader reader, scoped ref ulong value, MessagePackSerializerOptions options)
        => value = reader.ReadUInt64();

    public override void Write(ref MessagePackWriter writer, scoped ref ulong value, MessagePackSerializerOptions options)
        => writer.Write(value);

}

