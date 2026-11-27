using MemoryPack;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DataWF.Serialization;

public interface IValueTypeSerializer<T> : IElementSerializer<T>
    where T : struct
{
    void IElementSerializer.Read(XmlInvokeReader reader, ref object? value)
    {
        if (value == null)
        {
            var typedValue = default(T);
            Read(reader, ref typedValue);
            value = typedValue;
        }
        else
        {
            Read(reader, ref Unsafe.Unbox<T>(value));
        }

    }

    void IElementSerializer.Read(XmlInvokeReader reader, ref object value, ISerializePropertyInfo propertyInfo)
    {
        Read(reader, ref Unsafe.Unbox<T>(value), propertyInfo);
    }

    void IElementSerializer.Write(XmlInvokeWriter writer, ref object? value)
    {
        var typedValue = value == null ? default : Unsafe.Unbox<T>(value);
        Write(writer, ref typedValue);
    }

    void IElementSerializer.Write(XmlInvokeWriter writer, ref object value, ISerializePropertyInfo propertyInfo)
    {
        Write(writer, ref Unsafe.Unbox<T>(value), propertyInfo);
    }

    void IElementSerializer.Read(ref Utf8JsonReader reader, ref object? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            var typedValue = default(T);
            Read(ref reader, ref typedValue, options);
            value = typedValue;
        }
        else
        {
            Read(ref reader, ref Unsafe.Unbox<T>(value), options);
        }

    }

    void IElementSerializer.Read(ref Utf8JsonReader reader, ref object value, JsonSerializerOptions options,
        ISerializePropertyInfo property)
    {
        Read(ref reader, ref Unsafe.Unbox<T>(value), options, property);
    }

    void IElementSerializer.Write(Utf8JsonWriter writer, ref object? value, JsonSerializerOptions options)
    {
        var typedValue = value == null ? default(T) : Unsafe.Unbox<T>(value);
        Write(writer, ref typedValue, options);
    }

    void IElementSerializer.Write(Utf8JsonWriter writer, ref object value, JsonSerializerOptions options,
        ISerializePropertyInfo property)
    {
        Write(writer, ref Unsafe.Unbox<T>(value), options, property);
    }

    void IElementSerializer.Read(ref MemoryPackReader reader, ref object? value)
    {
        if (value == null)
        {
            var typedValue = default(T);
            Read(ref reader, ref typedValue);
            value = typedValue;
        }
        else
        {
            Read(ref reader, ref Unsafe.Unbox<T>(value));
        }
    }

    void IElementSerializer.Read(ref MemoryPackReader reader, ref object value, ISerializePropertyInfo propertyInfo)
    {
        Read(ref reader, ref Unsafe.Unbox<T>(value), propertyInfo);
    }

    void IElementSerializer.Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref object? value)
    {
        var typedValue = value == null ? default : Unsafe.Unbox<T>(value);
        Write(ref writer, ref typedValue);
    }

    void IElementSerializer.Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref object value,
        ISerializePropertyInfo propertyInfo)
    {
        Write(ref writer, ref Unsafe.Unbox<T>(value), propertyInfo);
    }

    string IElementSerializer.ToString(object? value)
    {
        var typedValue = value == null ? default : Unsafe.Unbox<T>(value);
        return ToString(typedValue);
    }

    string IElementSerializer.FormatDisplay(object? value, string? format)
    {
        var typedValue = value == null ? default : Unsafe.Unbox<T>(value);
        return FormatDisplay(typedValue!, format);
    }

    void IMemoryPackFormatter.Deserialize(ref MemoryPackReader reader, scoped ref object? value)
    {
        if (value == null)
        {
            var typedValue = default(T);
            Read(ref reader, ref typedValue);
            value = typedValue;
        }
        else
        {
            Read(ref reader, ref Unsafe.Unbox<T>(value));
        }
    }

    void IMemoryPackFormatter.Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer,
        scoped ref object? value)
    {
        var typedValue = value == null ? default : Unsafe.Unbox<T>(value);
        Write(ref writer, ref typedValue);
    }
}