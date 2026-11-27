using System;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class GuidSerializer : PrimitiveSerializer<Guid>
{
    public static readonly GuidSerializer Instance = new();

    public override Guid Read(DbDataReader reader, int i)
        => reader.GetGuid(i);

    public override void Read(XmlInvokeReader reader, scoped ref Guid value)
        => value = FromString(reader.ReadContentAsString());

    public override void Write(XmlInvokeWriter writer, scoped ref Guid value)
        => writer.WriteValue(ToString(value));

    public override void Read(ref Utf8JsonReader reader, scoped ref Guid value, JsonSerializerOptions options)
       => value = reader.TryGetGuid(out var rvalue) ? rvalue : Guid.Empty;

    public override void Write(Utf8JsonWriter writer, scoped ref Guid value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);

    public override void Write(Utf8JsonWriter writer, scoped ref Guid value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
        => writer.WriteString(propertyInfo.JsonName, value);

    public override void Read(ref MessagePackReader reader, scoped ref Guid value, MessagePackSerializerOptions options)
        => value = MessagePack.Formatters.GuidFormatter.Instance.Deserialize(ref reader, options);

    public override void Write(ref MessagePackWriter writer, scoped ref Guid value, MessagePackSerializerOptions options)
        => MessagePack.Formatters.GuidFormatter.Instance.Serialize(ref writer, value, options);

    public override Guid FromString(ReadOnlySpan<char> value)
        => Guid.TryParse(value, out var guid) ? guid : Guid.Empty;

    public override string ToString(Guid value)
        => value.ToString();

    public override string FormatDisplay(Guid value, string? format = null)
        => value.ToString(format, CultureInfo.InvariantCulture);

    public override string FormatQuery(Guid value, string? format = null)
        => $"'{value.ToString(format, CultureInfo.InvariantCulture).ToLower()}'";

    public override Guid Parse(object value)
    {
        if (value is Guid typedValue)
            return typedValue;
        if (value == null || value == DBNull.Value)
            return Guid.Empty;
        if (value is byte[] bytes)
            return new Guid(bytes);
        return Guid.TryParse(value.ToString(), out var guid) ? guid : Guid.Empty;
    }
}
