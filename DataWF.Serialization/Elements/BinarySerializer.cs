using System;
using System.Buffers;
using System.Text.Json;
using MessagePack;
using MemoryPack;

namespace DataWF.Serialization.Elements;

public class BinarySerializer<T> : ElementSerializer<T> where T : IBinarySerializable
{
    public override bool IsAttributeSerializer => false;

    public override string FormatDisplay(T value, string? format = null)
    {
        throw new NotImplementedException();
    }

    public override T FromString(ReadOnlySpan<char> value)
    {
        var buffer = ByteArraySerializer.Instance.FromString(value);
        return buffer == null ? default! : T.Deserialize<T>(buffer);
    }

    public override string ToString(T value)
    {
        var buffer = value.Serialize();
        return Convert.ToBase64String(buffer);
    }

    public override T Parse(object value)
    {
        return value switch
        {
            T same => same,
            byte[] buffer => T.Deserialize<T>(buffer),
            string str => FromString(str),
            null => default!,
            _ => throw new NotSupportedException($"Cannot parse {value.GetType()} to {typeof(T)}"),
        };
    }

    public override void Read(XmlInvokeReader reader, scoped ref T? value)
    {
        var array = default(byte[]);
        ByteArraySerializer.Instance.Read(reader, ref array);
        if (array != null)
            value = T.Deserialize<T>(array);
    }

    public override void Write(XmlInvokeWriter writer, scoped ref T? value)
    {
        var buffer = value?.Serialize() ?? default(byte[]);
        ByteArraySerializer.Instance.Write(writer, ref buffer);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref T? value, JsonSerializerOptions options)
    {
        var array = default(byte[]);
        ByteArraySerializer.Instance.Read(ref reader, ref array, options);
        if (array != null)
            value = T.Deserialize<T>(array);
    }

    public override void Write(Utf8JsonWriter writer, scoped ref T? value, JsonSerializerOptions options)
    {
        var buffer = value?.Serialize() ?? default(byte[]);
        ByteArraySerializer.Instance.Write(writer, ref buffer, options);
    }

    public override void Read(ref MemoryPackReader reader, scoped ref T? value)
    {
        var array = default(byte[]);
        ByteArraySerializer.Instance.Read(ref reader, ref array);
        if (array != null)
            value = T.Deserialize<T>(array);
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T? value)
    {
        var buffer = value?.Serialize() ?? default(byte[]);
        ByteArraySerializer.Instance.Write(ref writer, ref buffer);
    }

    public override void Read(ref MessagePackReader reader, scoped ref T? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = default;
            return;
        }

        var bytes = reader.ReadBytes()?.ToArray();
        value = bytes == null ? default : T.Deserialize<T>(bytes);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref T? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        writer.Write(value.Serialize());
    }
}
