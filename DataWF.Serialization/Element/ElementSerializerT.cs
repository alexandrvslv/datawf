using MemoryPack;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MessagePack;

namespace DataWF.Serialization;

[ExcludeFormatterFromSourceGeneratedResolver]
public abstract class ElementSerializer<T> : JsonConverter<T>, IElementSerializer<T>
{
    public static SerializeTypeInfo<T> GetTypeInfo()
    {
        throw new NotImplementedException();
    }

    protected const string stringNull = "null";
    protected const string stringXmlNil = "nil";
    protected const string stringXmlType = "Discriminator";
    public const string stringXmlKey = "Key";
    public const string stringXmlValue = "Value";
    protected const string stringJsonType = "&type";
    public static ReadOnlySpan<byte> stringJsonKey => "Key"u8;
    public static ReadOnlySpan<byte> stringJsonValue => "Value"u8;
    protected static readonly int[] ScaleFactors = [
        0,         // 0 знаков
        1000000,   // 1 знак  (.1 -> 1000000 тиков)
        100000,    // 2 знака (.12 -> 1200000 тиков)
        10000,     // 3 знака (.123 -> 1230000 тиков)
        1000,      // 4 знака ...
        100,       // 5 знаков
        10,        // 6 знаков
        1          // 7 знаков (.1234567 -> 1234567 тиков)
    ];
    protected IEqualityComparer<T> equalityComparer;
    protected ISerializeTypeInfo<T> typeInfo = null!;

    public ElementSerializer()
    {
        equalityComparer = EqualityComparer<T>.Default;
    }

    public ElementSerializer(ISerializeTypeInfo<T> typeInfo)
        :this()
    {
        TypeInfo = typeInfo;
    }

    public ISerializeTypeInfo<T> TypeInfo
    {
        get => typeInfo;
        set => typeInfo = value;
    }

    ISerializeTypeInfo IElementSerializer.TypeInfo
    {
        get => TypeInfo;
        set => TypeInfo = (ISerializeTypeInfo<T>)value;
    }

    public abstract bool IsAttributeSerializer { get; }

    public override bool CanConvert(Type typeToConvert)
        => base.CanConvert(typeToConvert);

    public abstract T FromString(ReadOnlySpan<char> value);

    public virtual T FromString(string value) => FromString(value.AsSpan());

    public abstract string ToString(T value);

    public virtual T Read(DbDataReader reader, int i)
        => reader.GetFieldValue<T>(i);

    public abstract void Read(XmlInvokeReader reader, scoped ref T? value);

    public abstract void Write(XmlInvokeWriter writer, scoped ref T? value);

    public abstract void Read(ref Utf8JsonReader reader, scoped ref T? value, JsonSerializerOptions options);

    public abstract void Write(Utf8JsonWriter writer, scoped ref T? value, JsonSerializerOptions options);

    public abstract void Read(ref MemoryPackReader reader, scoped ref T? value);

    public abstract void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T? value)
        where TBufferWriter : IBufferWriter<byte>;

    public abstract void Read(ref MessagePackReader reader, scoped ref T? value, MessagePackSerializerOptions options);
    
    public abstract void Write(ref MessagePackWriter writer, scoped ref T? value, MessagePackSerializerOptions options);
    
    public virtual void Read(XmlInvokeReader reader, scoped ref T value, ISerializePropertyInfo propertyInfo)
    {
        Read(reader, ref value!);
    }

    public virtual void Write(XmlInvokeWriter writer, scoped ref T value, ISerializePropertyInfo propertyInfo)
    {
        writer.WriteStart(propertyInfo);
        Write(writer, ref value!);
        writer.WriteEnd(propertyInfo);
    }

    public virtual void Read(ref Utf8JsonReader reader, scoped ref T value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        Read(ref reader, ref value!, options);
    }

    public virtual void Write(Utf8JsonWriter writer, scoped ref T value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        writer.WritePropertyName(propertyInfo.JsonName);
        Write(writer, ref value!, options);
    }

    public virtual void Read(ref MemoryPackReader reader, scoped ref T value, ISerializePropertyInfo propertyInfo)
    {
        Read(ref reader, ref value!);
    }

    public virtual void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T value, ISerializePropertyInfo propertyInfo)
        where TBufferWriter : IBufferWriter<byte>
    {
        Write(ref writer, ref value!);
    }

    public virtual void Read(ref MessagePackReader reader, scoped ref T value, MessagePackSerializerOptions options, ISerializePropertyInfo info)
    {
        Read(ref reader, ref value!, options);
    }

    public virtual void Write(ref MessagePackWriter writer, scoped ref T value, MessagePackSerializerOptions options, ISerializePropertyInfo info)
    {
        Write(ref writer, ref value!, options);
    }


    public sealed override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        Write(writer, ref value!, options);
    }

    public sealed override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var value = default(T);
        Read(ref reader, ref value, options);
        return value!;
    }

    public virtual string FormatQuery(T value, string? format = null)
    {
        if (value is IFormattable formattable)
        {
            return formattable.ToString(format, CultureInfo.InvariantCulture);
        }
        return $"'{ToString(value)}'";
    }

    public abstract string FormatDisplay(T value, string? format = null);

    public abstract T Parse(object value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool Equal(T oldValue, T newValue)
    {
        return equalityComparer.Equals(oldValue, newValue);
    }

    public static string EscapeJsonControlCharacters(string json)
    {
        StringBuilder? result = null;
        var inString = false;
        var escaped = false;
        for (var i = 0; i < json.Length; i++)
        {
            var current = json[i];
            if (inString && current < ' ')
            {
                result ??= new StringBuilder(json.Length + 8).Append(json, 0, i);
                result.Append("\\u00").Append(((int)current).ToString("X2", CultureInfo.InvariantCulture));
                continue;
            }

            result?.Append(current);
            if (!inString)
            {
                inString = current == '"';
            }
            else if (escaped)
            {
                escaped = false;
            }
            else if (current == '\\')
            {
                escaped = true;
            }
            else if (current == '"')
            {
                inString = false;
            }
        }

        return result?.ToString() ?? json;
    }

    public IElementSerializer<T> WithTypeInfo(ISerializeTypeInfo<T> serializeTypeInfo)
    {
        var clone = (ElementSerializer<T>)MemberwiseClone();
        clone.typeInfo = serializeTypeInfo;
        return clone;
    }

    
}

