using DataWF.Invokers;
using MemoryPack;
using MessagePack;
using System;
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.Serialization;

[Invoker(Instance = true)]
public partial interface ISerializePropertyInfo : IInvoker
{
    JsonEncodedText JsonName { get; }
    bool HasDefault { get; }
    object? DefaultValue { get; set; }
    bool IsAttribute { get; }
    bool IsXmlAttributed { get; }
    bool IsChangeSensitive { get; }
    bool IsReadOnly { get; }
    bool IsRequired { get; }
    bool IsText { get; }
    bool IsNonSerialize { get; }
    int Order { get; set; }
    IInvoker Invoker { get; set; }
    IElementSerializer Serializer { get; set; }

    JsonIgnoreCondition? IgnoreCondition { get; set; }

    bool CheckDefault(ref object value);

    void Read(XmlInvokeReader reader, ref object value);

    void Write(XmlInvokeWriter writer, ref object value);

    void Read(ref Utf8JsonReader reader, ref object value, JsonSerializerOptions options);

    void Write(Utf8JsonWriter writer, ref object value, JsonSerializerOptions options);

    void Read(ref MemoryPackReader reader, ref object value);

    void Write<TBuffer>(ref MemoryPackWriter<TBuffer> writer, ref object value)
        where TBuffer : IBufferWriter<byte>;

    void Read(ref MessagePackReader reader, ref object value, MessagePackSerializerOptions options);
    void Write(ref MessagePackWriter writer, ref object value, MessagePackSerializerOptions options);

    void ValidateSerializer();

}

public interface ISerializePropertyInfo<T> : ISerializePropertyInfo
//where T : allows ref struct
{
    void Copy(scoped ref T obj, scoped ref T newItem);

    bool CheckDefault(scoped ref T value);

    bool ISerializePropertyInfo.CheckDefault(ref object value)
    {
        var typedValue = (T)value;
        return CheckDefault(ref typedValue);
    }

    void Read(XmlInvokeReader reader, scoped ref T value);

    void ISerializePropertyInfo.Read(XmlInvokeReader reader, ref object value)
    {
        var typed = (T)value;
        Read(reader, ref typed);
        value = typed!;
    }

    void Write(XmlInvokeWriter writer, scoped ref T value);

    void ISerializePropertyInfo.Write(XmlInvokeWriter writer, ref object value)
    {
        var typed = (T)value;
        Write(writer, ref typed);
    }

    void Read(ref Utf8JsonReader reader, scoped ref T value, JsonSerializerOptions options);

    void ISerializePropertyInfo.Read(ref Utf8JsonReader reader, ref object value, JsonSerializerOptions options)
    {
        var typed = (T)value;
        Read(ref reader, ref typed, options);
        value = typed!;
    }

    void Write(Utf8JsonWriter writer, scoped ref T value, JsonSerializerOptions options);

    void ISerializePropertyInfo.Write(Utf8JsonWriter writer, ref object value, JsonSerializerOptions options)
    {
        var typed = (T)value;
        Write(writer, ref typed, options);
    }

    void Read(ref MemoryPackReader reader, scoped ref T value);

    void ISerializePropertyInfo.Read(ref MemoryPackReader reader, ref object value)
    {
        var typed = (T)value;
        Read(ref reader, ref typed);
        value = typed!;
    }

    void Write<TBuffer>(ref MemoryPackWriter<TBuffer> writer, scoped ref T value)
        where TBuffer : IBufferWriter<byte>;

    void Read(ref MessagePackReader reader, scoped ref T value, MessagePackSerializerOptions options);

    void Write(ref MessagePackWriter writer, scoped ref T value, MessagePackSerializerOptions options);

    void ISerializePropertyInfo.Write<TBuffer>(ref MemoryPackWriter<TBuffer> writer, ref object value)
    {
        var typed = (T)value;
        Write(ref writer, ref typed);
    }

    void ISerializePropertyInfo.Read(ref MessagePackReader reader, ref object value, MessagePackSerializerOptions options)
    {
        var typed = (T)value;
        Read(ref reader, ref typed, options);
        value = typed!;
    }

    void ISerializePropertyInfo.Write(ref MessagePackWriter writer, ref object value, MessagePackSerializerOptions options)
    {
        var typed = (T)value;
        Write(ref writer, ref typed, options);
        value = typed!;
    }

    ISerializePropertyInfo<T> WithOrder(int i);
}

public interface ISerializePropertyInfo<T, V> : ISerializePropertyInfo<T>, IInvoker<T, V>
{
    new IInvoker<T, V> Invoker { get; set; }

    IInvoker ISerializePropertyInfo.Invoker
    {
        get => Invoker;
        set => Invoker = (IInvoker<T, V>)value;
    }

    new V? DefaultValue { get; set; }

    object? ISerializePropertyInfo.DefaultValue
    {
        get => DefaultValue;
        set => DefaultValue = (V?)value;
    }

    new IElementSerializer<V> Serializer { get; set; }

    IElementSerializer ISerializePropertyInfo.Serializer
    {
        get => Serializer;
        set => Serializer = (IElementSerializer<V>)value;
    }

    bool CheckDefault(V value);

    void ISerializePropertyInfo<T>.Read(XmlInvokeReader reader, scoped ref T value)
    {
        var entry = Invoker.GetValue(in value);
        Serializer.Read(reader, ref entry);
        Invoker.SetValue(ref value, entry!);
    }

    void ISerializePropertyInfo<T>.Write(XmlInvokeWriter writer, scoped ref T value)
    {
        var ignore = IgnoreCondition ?? writer.DefaultIgnoreCondition;
        if (ignore == JsonIgnoreCondition.WhenWriting)
            return;

        var entry = Invoker.GetValue(in value);
        if (ignore is JsonIgnoreCondition.WhenWritingNull
                or JsonIgnoreCondition.WhenWritingDefault
            && entry == null)
            return;

        if (ignore == JsonIgnoreCondition.WhenWritingDefault
            && CheckDefault(entry))
            return;

        Serializer.Write(writer, ref entry, this);
    }

    void ISerializePropertyInfo<T>.Read(ref Utf8JsonReader reader, scoped ref T value, JsonSerializerOptions options)
    {
        var entry = Invoker.GetValue(in value);
        Serializer.Read(ref reader, ref entry, options);
        Invoker.SetValue(ref value, entry!);
    }

    void ISerializePropertyInfo<T>.Write(Utf8JsonWriter writer, scoped ref T value, JsonSerializerOptions options)
    {
        var ignore = IgnoreCondition ?? options.DefaultIgnoreCondition;
        if (ignore == JsonIgnoreCondition.WhenWriting)
            return;

        var entry = Invoker.GetValue(in value);
        if (ignore is JsonIgnoreCondition.WhenWritingNull
                or JsonIgnoreCondition.WhenWritingDefault
            && entry == null)
            return;

        if (ignore == JsonIgnoreCondition.WhenWritingDefault
            && CheckDefault(entry))
            return;

        Serializer.Write(writer, ref entry, options, this);
    }

    void ISerializePropertyInfo<T>.Read(ref MemoryPackReader reader, scoped ref T value)
    {
        var entry = Invoker.GetValue(in value);
        Serializer.Read(ref reader, ref entry);
        Invoker.SetValue(ref value, entry!);
    }

    void ISerializePropertyInfo<T>.Write<TBuffer>(ref MemoryPackWriter<TBuffer> writer, scoped ref T value)
    // where TBuffer : IBufferWriter<byte>
    {
        var entry = Invoker.GetValue(in value);
        Serializer.Write(ref writer, ref entry);
    }

    void ISerializePropertyInfo<T>.Read(ref MessagePackReader reader, scoped ref T value, MessagePackSerializerOptions options)
    {
        var entry = Invoker.GetValue(in value);
        Serializer.Read(ref reader, ref entry, options);
        Invoker.SetValue(ref value, entry!);
    }

    void ISerializePropertyInfo<T>.Write(ref MessagePackWriter writer, scoped ref T value, MessagePackSerializerOptions options)
    {
        var entry = Invoker.GetValue(in value);
        Serializer.Write(ref writer, ref entry, options);
    }
}