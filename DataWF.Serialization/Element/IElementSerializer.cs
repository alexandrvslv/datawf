using MemoryPack;
using MessagePack;
using MessagePack.Formatters;
using System;
using System.Buffers;
using System.Data.Common;
using System.Text.Json;

namespace DataWF.Serialization;

public interface IElementSerializer : IMemoryPackFormatter, IMessagePackFormatter
{
    bool IsAttributeSerializer { get; }
    ISerializeTypeInfo TypeInfo { get; set; }
    object? Read(DbDataReader reader, int i);

    object? FromString(ReadOnlySpan<char> value);
    string ToString(object? value);
    string FormatDisplay(object? value, string? format = null);

    void Read(XmlInvokeReader reader, ref object? value);
    void Read(XmlInvokeReader reader, ref object value, ISerializePropertyInfo info);
    void Write(XmlInvokeWriter writer, ref object? value);
    void Write(XmlInvokeWriter writer, ref object value, ISerializePropertyInfo info);

    void Read(ref Utf8JsonReader reader, ref object? value, JsonSerializerOptions options);
    void Read(ref Utf8JsonReader reader, ref object value, JsonSerializerOptions options, ISerializePropertyInfo info);
    void Write(Utf8JsonWriter writer, ref object? value, JsonSerializerOptions options);
    void Write(Utf8JsonWriter writer, ref object value, JsonSerializerOptions options, ISerializePropertyInfo info);

    void Read(ref MemoryPackReader reader, ref object? value);
    void Read(ref MemoryPackReader reader, ref object value, ISerializePropertyInfo info);
    void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref object? value)
        where TBufferWriter : IBufferWriter<byte>;
    void Write<TBuffer>(ref MemoryPackWriter<TBuffer> writer, ref object value, ISerializePropertyInfo info)
        where TBuffer : IBufferWriter<byte>;

    void Read(ref MessagePackReader reader, ref object? value, MessagePackSerializerOptions options);
    void Read(ref MessagePackReader reader, ref object value, MessagePackSerializerOptions options, ISerializePropertyInfo info);
    void Write(ref MessagePackWriter writer, ref object? value, MessagePackSerializerOptions options);
    void Write(ref MessagePackWriter writer, ref object value, MessagePackSerializerOptions options, ISerializePropertyInfo info);

    IElementSerializer WithTypeInfo(ISerializeTypeInfo typeInfo);
}
