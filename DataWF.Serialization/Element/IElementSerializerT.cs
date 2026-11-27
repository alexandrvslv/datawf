using MemoryPack;
using System;
using System.Buffers;
using System.Data.Common;
using System.Text.Json;
using MessagePack;
using MessagePack.Formatters;

namespace DataWF.Serialization;

public interface IElementSerializer<T> : IMemoryPackFormatter<T>, IMessagePackFormatter<T>, IElementSerializer
{
    static abstract SerializeTypeInfo<T> GetTypeInfo();
    new ISerializeTypeInfo<T> TypeInfo { get; set; }
    string FormatDisplay(T value, string? format = null);
    new T FromString(ReadOnlySpan<char> value);
    string ToString(T value);
    T Parse(object value);

    new T Read(DbDataReader reader, int i);

    void Read(XmlInvokeReader reader, scoped ref T? value);
    void Read(XmlInvokeReader reader, scoped ref T value, ISerializePropertyInfo property);
    void Write(XmlInvokeWriter writer, scoped ref T? value);
    void Write(XmlInvokeWriter writer, scoped ref T value, ISerializePropertyInfo property);

    void Read(ref Utf8JsonReader reader, scoped ref T? value, JsonSerializerOptions options);

    void Read(ref Utf8JsonReader reader, scoped ref T value, JsonSerializerOptions options,
        ISerializePropertyInfo property);

    void Write(Utf8JsonWriter writer, scoped ref T? value, JsonSerializerOptions options);

    void Write(Utf8JsonWriter writer, scoped ref T value, JsonSerializerOptions options,
        ISerializePropertyInfo property);

    void Read(ref MemoryPackReader reader, scoped ref T? value);
    void Read(ref MemoryPackReader reader, scoped ref T value, ISerializePropertyInfo property);

    void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T? value)
        where TBufferWriter : IBufferWriter<byte>;

    void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T value,
        ISerializePropertyInfo property)
        where TBufferWriter : IBufferWriter<byte>;

    void Read(ref MessagePackReader reader, scoped ref T? value, MessagePackSerializerOptions options);
    void Read(ref MessagePackReader reader, scoped ref T value, MessagePackSerializerOptions options, ISerializePropertyInfo property);
    void Write(ref MessagePackWriter writer, scoped ref T? value, MessagePackSerializerOptions options);
    void Write(ref MessagePackWriter writer, scoped ref T value, MessagePackSerializerOptions options, ISerializePropertyInfo property);

    object? IElementSerializer.Read(DbDataReader reader, int i) => Read(reader, i);

    void IElementSerializer.Read(XmlInvokeReader reader, ref object? value)
    {
        var typedValue = value == null ? default : (T)value;
        Read(reader, ref typedValue);
        value = typedValue;
    }

    void IElementSerializer.Read(XmlInvokeReader reader, ref object value, ISerializePropertyInfo propertyInfo)
    {
        var typedValue = (T)value;
        Read(reader, ref typedValue, propertyInfo);
    }

    void IElementSerializer.Write(XmlInvokeWriter writer, ref object? value)
    {
        var typedValue = value == null ? default : (T)value;
        Write(writer, ref typedValue!);
    }

    void IElementSerializer.Write(XmlInvokeWriter writer, ref object value, ISerializePropertyInfo propertyInfo)
    {
        var typedValue = (T)value;
        Write(writer, ref typedValue, propertyInfo);
    }

    void IElementSerializer.Read(ref Utf8JsonReader reader, ref object? value, JsonSerializerOptions options)
    {
        var typedValue = value == null ? default : (T)value;
        Read(ref reader, ref typedValue, options);
        value = typedValue;
    }

    void IElementSerializer.Read(ref Utf8JsonReader reader, ref object value, JsonSerializerOptions options,
        ISerializePropertyInfo property)
    {
        var typedValue = (T)value;
        Read(ref reader, ref typedValue, options, property);
        value = typedValue!;
    }

    void IElementSerializer.Write(Utf8JsonWriter writer, ref object? value, JsonSerializerOptions options)
    {
        var typedValue = value == null ? default : (T)value;
        Write(writer, ref typedValue!, options);
    }

    void IElementSerializer.Write(Utf8JsonWriter writer, ref object value, JsonSerializerOptions options,
        ISerializePropertyInfo property)
    {
        var typedValue = (T)value;
        Write(writer, ref typedValue!, options, property);
    }

    void IElementSerializer.Read(ref MemoryPackReader reader, ref object? value)
    {
        var typedValue = value == null ? default : (T)value;
        Read(ref reader, ref typedValue);
        value = typedValue;
    }

    void IElementSerializer.Read(ref MemoryPackReader reader, ref object value, ISerializePropertyInfo propertyInfo)
    {
        var typedValue = (T)value;
        Read(ref reader, ref typedValue, propertyInfo);
        value = typedValue!;
    }

    void IElementSerializer.Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref object? value)
    {
        var typedValue = value == null ? default : (T)value;
        Write(ref writer, ref typedValue!);
    }

    void IElementSerializer.Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref object value,
        ISerializePropertyInfo propertyInfo)
    {
        var typedValue = (T)value;
        Write(ref writer, ref typedValue, propertyInfo);
    }

    void IElementSerializer.Read(ref MessagePackReader reader, ref object? value, MessagePackSerializerOptions options)
    {
        var typedValue = value == null ? default : (T)value;
        Read(ref reader, ref typedValue, options);
        value = typedValue;
    }

    void IElementSerializer.Read(ref MessagePackReader reader, ref object value, MessagePackSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        var typedValue = (T)value;
        Read(ref reader, ref typedValue, options, propertyInfo);
        value = typedValue!;
    }

    void IElementSerializer.Write(ref MessagePackWriter writer, ref object? value, MessagePackSerializerOptions options)
    {
        var typedValue = value == null ? default : (T)value;
        Write(ref writer, ref typedValue!, options);
    }

    void IElementSerializer.Write(ref MessagePackWriter writer, ref object value, MessagePackSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        var typedValue = (T)value;
        Write(ref writer, ref typedValue!, options, propertyInfo);
    }

    object? IElementSerializer.FromString(ReadOnlySpan<char> value) => FromString(value);

    string IElementSerializer.ToString(object? value)
    {
        var typedValue = value == null ? default : (T)value;
        return ToString(typedValue!);
    }

    string IElementSerializer.FormatDisplay(object? value, string? format)
    {
        var typedValue = value == null ? default : (T)value;
        return FormatDisplay(typedValue!, format);
    }

    void IMemoryPackFormatter.Deserialize(ref MemoryPackReader reader, scoped ref object? value)
    {
        var typedValue = value == null ? default : (T)value;
        Read(ref reader, ref typedValue);
        value = typedValue;
    }

    void IMemoryPackFormatter.Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer,
        scoped ref object? value)
    {
        var typedValue = value == null ? default : (T)value;
        Write(ref writer, ref typedValue);
    }

    void IMemoryPackFormatter<T>.Deserialize(ref MemoryPackReader reader, scoped ref T? value)
        => Read(ref reader, ref value);

    void IMemoryPackFormatter<T>.Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T? value)
        => Write(ref writer, ref value!);

    void IMessagePackFormatter<T>.Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
    {
        Write(ref writer, ref value!, options);
    }

    T IMessagePackFormatter<T>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var value = default(T);
        Read(ref reader, ref value, options);
        return value!;
    }

    IElementSerializer<T> WithTypeInfo(ISerializeTypeInfo<T> serializeTypeInfo);

    IElementSerializer IElementSerializer.WithTypeInfo(ISerializeTypeInfo typeInfo) => WithTypeInfo((ISerializeTypeInfo<T>)typeInfo);
}
