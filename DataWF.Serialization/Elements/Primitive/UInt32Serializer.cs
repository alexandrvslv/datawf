using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class UInt32Serializer : PrimitiveSerializer<uint>
{
    public static readonly UInt32Serializer Instance = new();

    public override bool IsAttributeSerializer => true;

    public override uint Read(DbDataReader reader, int i)
        => (uint)reader.GetInt64(i);

    public override uint FromString(ReadOnlySpan<char> value)
        => uint.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : (uint)0;

    public override string ToString(uint value)
        => value.ToString(CultureInfo.InvariantCulture);

    public override string FormatDisplay(uint value, string? format = null)
        => value.ToString(format, CultureInfo.InvariantCulture);

    public override uint Parse(object? value)
        => Convert.ToUInt32(value);

    public override void Read(XmlInvokeReader reader, scoped ref uint value)
        => value = (uint)reader.ReadContentAsLong();

    public override void Write(XmlInvokeWriter writer, scoped ref uint value)
        => writer.WriteValue(value);

    public override void Read(ref Utf8JsonReader reader, scoped ref uint value, JsonSerializerOptions options)
        => value = reader.TryGetUInt32(out var rvalue) ? rvalue : 0;

    public override void Write(Utf8JsonWriter writer, scoped ref uint value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);

    public override void Write(Utf8JsonWriter writer, scoped ref uint value, JsonSerializerOptions options,
        ISerializePropertyInfo propertyInfo)
        => writer.WriteNumber(propertyInfo.JsonName, value);
    
    public override void Read(ref MessagePackReader reader, scoped ref uint value, MessagePackSerializerOptions options)
        => value = reader.ReadUInt32();

    public override void Write(ref MessagePackWriter writer, scoped ref uint value, MessagePackSerializerOptions options)
        => writer.Write(value);
}