using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization.Elements;

public class TypeInfoSerializer<T> : ElementSerializer<T>
{
    protected static uint GetJsonPropertyNameHash(ref Utf8JsonReader reader)
    {
        if (!reader.ValueIsEscaped && !reader.HasValueSequence)
            return GetJsonPropertyNameHash(reader.ValueSpan);
        return GetJsonPropertyNameHashSlow(ref reader);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected static uint GetJsonPropertyNameHashSlow(ref Utf8JsonReader reader)
    {
        if (reader.ValueIsEscaped)
        {
            var length = (int)(reader.HasValueSequence
                ? reader.ValueSequence.Length
                : reader.ValueSpan.Length);
            byte[]? rentedBytes = null;
            try
            {
                Span<byte> buffer = length < 200
                    ? stackalloc byte[length]
                    : (rentedBytes = ArrayPool<byte>.Shared.Rent(length));
                var bytesWritten = reader.CopyString(buffer);
                return GetJsonPropertyNameHash(buffer[..bytesWritten]);
            }
            finally
            {
                if (rentedBytes != null)
                    ArrayPool<byte>.Shared.Return(rentedBytes);
            }
        }

        var hash = 2166136261u;
        foreach (var segment in reader.ValueSequence)
        {
            AppendNameHash(segment.Span, ref hash);
        }

        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetJsonPropertyNameHash(ReadOnlySpan<byte> name)
    {
        var hash = 2166136261u;
        AppendNameHash(name, ref hash);
        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendNameHash(ReadOnlySpan<byte> name, ref uint hash)
    {
        int i = 0;
        int length = name.Length;
        for (; i < length; i++)
        {
            var b = name[i];
            if (b > 0x7f)
                goto SlowPath;

            if (b >= 65 && b <= 90)
            {
                b |= 0x20;
            }

            hash = (hash ^ 0u) * 16777619u;
            hash = (hash ^ b) * 16777619u;
        }
        return;

        SlowPath:
        AppendNameHashSlow(name.Slice(i), ref hash);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AppendNameHashSlow(ReadOnlySpan<byte> name, ref uint hash)
    {
        Span<char> chars = stackalloc char[name.Length];
        int charsWritten = System.Text.Encoding.UTF8.GetChars(name, chars);
        ReadOnlySpan<char> decodedChars = chars.Slice(0, charsWritten);

        for (int j = 0; j < decodedChars.Length; j++)
        {
            char value = char.ToLowerInvariant(decodedChars[j]);
            hash = (hash ^ (byte)(value >> 8)) * 16777619u;
            hash = (hash ^ (byte)value) * 16777619u;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool JsonPropertyNameEquals(ReadOnlySpan<byte> runtimeSpan, ReadOnlySpan<byte> expectedLowerName)
    {
        if (runtimeSpan.Length != expectedLowerName.Length)
            return false;

        int i = 0;
        int length = runtimeSpan.Length;

        if (length >= 8)
        {
            ref byte runRef = ref MemoryMarshal.GetReference(runtimeSpan);
            ref byte expRef = ref MemoryMarshal.GetReference(expectedLowerName);

            for (; i <= length - 8; i += 8)
            {
                ulong runBlock = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref runRef, i));
                ulong expBlock = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref expRef, i));

                if ((runBlock & 0x8080808080808080UL) != 0)
                    goto SlowPath;

                ulong stringsLessThanA = runBlock + 0x3F3F3F3F3F3F3F3FUL;
                ulong stringsLessThanZ = runBlock + 0x2525252525252525UL;
                ulong isWithInRange = (stringsLessThanA ^ stringsLessThanZ) & 0x8080808080808080UL;
                runBlock |= (isWithInRange >> 2);

                if (runBlock != expBlock)
                    return false;
            }
        }

        for (; i < length; i++)
        {
            byte r = runtimeSpan[i];
            if (r > 0x7F)
                goto SlowPath;

            if (r >= 65 && r <= 90)
                r |= 0x20;

            if (r != expectedLowerName[i])
                return false;
        }

        return true;

        SlowPath:
        return SequenceEqualUtf8Slow(runtimeSpan.Slice(i), expectedLowerName.Slice(i));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SequenceEqualUtf8Slow(ReadOnlySpan<byte> runSpan, ReadOnlySpan<byte> expSpan)
    {
        // Для не-ASCII используем декодирование на стеке и стандартный OrdinalIgnoreCase
        Span<char> runChars = stackalloc char[runSpan.Length];
        int runWritten = System.Text.Encoding.UTF8.GetChars(runSpan, runChars);

        Span<char> expChars = stackalloc char[expSpan.Length];
        int expWritten = System.Text.Encoding.UTF8.GetChars(expSpan, expChars);

        return MemoryExtensions.Equals(
            runChars.Slice(0, runWritten),
            expChars.Slice(0, expWritten),
            StringComparison.OrdinalIgnoreCase
        );
    }

    protected static bool JsonPropertyNameEquals(ref Utf8JsonReader reader, ReadOnlySpan<byte> expected)
    {
        if (!reader.ValueIsEscaped && !reader.HasValueSequence)
            return JsonPropertyNameEquals(reader.ValueSpan, expected);
        return JsonPropertyNameEqualsSlow(ref reader, expected);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected static bool JsonPropertyNameEqualsSlow(ref Utf8JsonReader reader, ReadOnlySpan<byte> expected)
    {
        if (reader.ValueIsEscaped)
        {
            var length = (int)(reader.HasValueSequence
                ? reader.ValueSequence.Length
                : reader.ValueSpan.Length);
            byte[]? rentedBytes = null;
            try
            {
                Span<byte> buffer = length < 200
                    ? stackalloc byte[length]
                    : (rentedBytes = ArrayPool<byte>.Shared.Rent(length));
                var bytesWritten = reader.CopyString(buffer);
                return JsonPropertyNameEquals(buffer[..bytesWritten], expected);
            }
            finally
            {
                if (rentedBytes != null)
                    ArrayPool<byte>.Shared.Return(rentedBytes);
            }
        }

        var index = 0;
        foreach (var segment in reader.ValueSequence)
        {
            if (!JsonPropertyNameEquals(segment.Span,
                    expected.Slice(index, segment.Length)))
                return false;
            index += segment.Length;
        }

        return true;
    }

    protected ISerializePropertyInfo<T>? ReadProperty(ref Utf8JsonReader reader,
        ISerializePropertyInfo<T>[] properties,
        ref Dictionary<string, ISerializePropertyInfo<T>>.AlternateLookup<ReadOnlySpan<byte>> lookup,
        ref int index)
    {
        if (!reader.HasValueSequence && !reader.ValueIsEscaped)
        {
            return ReadNextProperty(reader.ValueSpan, properties, ref index)
                   ?? (lookup.TryGetValue(reader.ValueSpan, out var property) ? property : null);
        }

        return ReadPropertySlow(ref reader, properties, ref lookup, ref index);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected ISerializePropertyInfo<T>? ReadPropertySlow(ref Utf8JsonReader reader,
        ISerializePropertyInfo<T>[] properties,
        ref Dictionary<string, ISerializePropertyInfo<T>>.AlternateLookup<ReadOnlySpan<byte>> lookup,
        ref int index)
    {
        var length = (int)(reader.HasValueSequence
            ? reader.ValueSequence.Length
            : reader.ValueSpan.Length);
        byte[]? bytes = null;
        try
        {
            Span<byte> buffer = length < 200
                ? stackalloc byte[length]
                : (bytes = ArrayPool<byte>.Shared.Rent(length));
            int bytesWritten = reader.CopyString(buffer);

            return ReadNextProperty(buffer[..bytesWritten], properties, ref index)
                   ?? (lookup.TryGetValue(buffer[..bytesWritten], out var property) ? property : null);
        }
        finally
        {
            if (bytes != null)
                ArrayPool<byte>.Shared.Return(bytes);
        }
    }


    protected ISerializePropertyInfo<T>? ReadNextProperty(ReadOnlySpan<byte> name, ISerializePropertyInfo<T>[] properties, ref int index)
    {
        if ((uint)index >= (uint)properties.Length)
            return null;

        var property = properties[index];
        if (!Utf8StringOrdinalIgnoreCaseComparer.Instance.Equals(name, property.JsonName.EncodedUtf8Bytes))
            return null;

        index++;
        return property;
    }

    public TypeInfoSerializer()
        : this(new SerializeTypeReflectionInfo<T>())
    {
    }

    public TypeInfoSerializer(ISerializeTypeInfo<T> typeInfo)
        : base(typeInfo)
    {
    }

    public override bool IsAttributeSerializer => false;

    public override void Read(XmlInvokeReader reader, ref T? value)
    {
        var typeInfo = TypeInfo;
        if (reader.HasAttributes)
        {
            while (reader.MoveToNextAttribute())
            {
                if (reader.Name == stringXmlNil)
                {
                    if (reader.ReadContentAsBoolean())
                    {
                        value = default;
                        reader.MoveToElement();
                        return;
                    }
                }
                else
                {
                    value ??= typeInfo.Constructor.Create();
                    reader.ReadAttribute(ref value, typeInfo);
                }
            }

            reader.MoveToElement();
        }

        value ??= typeInfo.Constructor.Create();

        reader.ReadElements<T>(ref value, typeInfo);
    }

    public override void Write(XmlInvokeWriter writer, ref T? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var typeInfo = TypeInfo;
        foreach (var property in typeInfo.XmlProperties.AsSpan())
        {
            property.Write(writer, ref value);
        }
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref T? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            value = default;
            return;
        }

        var lookup = TypeInfo.GetLookup();
        var binaryIndex = 0;
        var properties = TypeInfo.SerializeProperties;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                reader.Skip();
                continue;
            }

            var property = ReadProperty(ref reader, properties, ref lookup, ref binaryIndex);
            reader.Read();

            //if (propertyName == "$ref") "$type" "$id"
            //{ }
            if (property == null)
            {
                reader.TrySkip();
                continue;
            }

            value ??= TypeInfo.Constructor.Create();
            property.Read(ref reader, ref value, options);
        }
    }

    public override void Write(Utf8JsonWriter writer, ref T? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        foreach (var property in TypeInfo.SerializeProperties.AsSpan())
        {
            property.Write(writer, ref value, options);
        }

        writer.WriteEndObject();
    }

    public override void Read(ref MemoryPackReader reader, scoped ref T? value)
    {
        if (!reader.TryReadObjectHeader(out var count))
        {
            value = default;
            return;
        }

        value ??= TypeInfo.Constructor.Create();
        var properties = TypeInfo.SerializeProperties;
        for (int i = 0; i < count; i++)
        {
            if (i >= properties.Length)
            {
                throw new IndexOutOfRangeException();
            }

            properties[i].Read(ref reader, ref value);
        }
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T? value)
    {
        if (value == null)
        {
            writer.WriteNullObjectHeader();
            return;
        }

        var properties = TypeInfo.SerializeProperties;
        writer.WriteObjectHeader((byte)properties.Length);
        foreach (var property in properties.AsSpan())
        {
            property.Write(ref writer, ref value);
        }
    }

    public override void Read(ref MessagePackReader reader, scoped ref T? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = default;
            return;
        }

        var count = reader.ReadArrayHeader();
        value ??= TypeInfo.Constructor.Create();
        var properties = TypeInfo.SerializeProperties;
        for (var i = 0; i < count; i++)
        {
            if (i >= properties.Length)
            {
                reader.Skip();
                continue;
            }

            properties[i].Read(ref reader, ref value, options);
        }
    }

    public override void Write(ref MessagePackWriter writer, scoped ref T? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        var properties = TypeInfo.SerializeProperties;
        writer.WriteArrayHeader(properties.Length);
        foreach (var property in properties.AsSpan())
        {
            property.Write(ref writer, ref value, options);
        }
    }

    public override T FromString(ReadOnlySpan<char> value)
        => JsonSerializer.Deserialize<T>(value, options: new JsonSerializerOptions { Converters = { this } })!;

    public override string ToString(T value)
        => JsonSerializer.Serialize(value, options: new JsonSerializerOptions { Converters = { this } });

    public override string FormatDisplay(T value, string? format = null)
    {
        var stringBuilder = new System.Text.StringBuilder();
        stringBuilder.Append('{');
        foreach (var property in TypeInfo.SerializeProperties.AsSpan())
        {
            stringBuilder.Append(property.Name);
            stringBuilder.Append(':').Append(' ');
            var propertyValue = property.Invoker.GetValue(value);
            stringBuilder.Append(property.Serializer.FormatDisplay(propertyValue));
            stringBuilder.Append(',').Append('\n');
        }

        stringBuilder.Append('}');
        return stringBuilder.ToString();
    }

    public override T Parse(object value)
        => value switch
        {
            T same => same,
            null => default!,
            _ => FromString(value.ToString())
        };
}