using System;
using System.Text.Json;
using DataWF.Invokers;
using DataWF.Invokers.Defaults;
using MessagePack;
using MemoryPack;
using System.Runtime.CompilerServices;
using System.Xml;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public class NullableSerializer<T> : ElementSerializer<T?>
    where T : struct
{
    public static readonly NullableSerializer<T> Instance = new();

    public static new ISerializeTypeInfo<T?> GetTypeInfo()
    {
        return new SerializeTypeInfo<T?>(
            typeName: nameof(Nullable),
            shortName: nameof(Nullable) + typeof(T).FormatShort(),
            constructor: null!,
            properties: [
                new SerializePropertyInfo<T?, bool>(
                    invoker: NullableInvokers<T>.HasValueInvoker.Instance,
                    keys: SerializePropertyKeys.ReadOnlyAttribute),
                new SerializePropertyInfo<T?, T>(
                    invoker: NullableInvokers<T>.ValueInvoker.Instance,
                    keys: SerializePropertyKeys.ReadOnlyNonSerializeAttribute)
            ]
        );
    }

    private IElementSerializer<T>? valueSerializer;

    public NullableSerializer(IElementSerializer<T>? valueSerializer)
        : this()
    {
        this.valueSerializer = valueSerializer;
    }

    public NullableSerializer()
        : base(GetTypeInfo())
    { }

    public IElementSerializer<T> ValueSerializer => valueSerializer ??= SerializeProvider.GetSerializer<T>();

    public override bool IsAttributeSerializer => ValueSerializer.IsAttributeSerializer;

    public override string FormatDisplay(T? value, string? format = null)
        => value.HasValue ? ValueSerializer.FormatDisplay(value.Value) : stringNull;

    public override string ToString(T? value)
        => value.HasValue ? ValueSerializer.FormatDisplay(value.Value) : string.Empty;

    public override T? FromString(ReadOnlySpan<char> value)
        => value.IsEmpty ? null : ValueSerializer.FromString(value);

    public override T? Parse(object value)
        => value switch
        {
            null => null,
            T typed => typed,
            _ => ValueSerializer.Parse(value)
        };

    public override void Read(XmlInvokeReader reader, scoped ref T? value)
    {
        if (reader.NodeType == XmlNodeType.Element)
        {
            if (reader.HasAttributes)
            {
                if (reader.MoveToNextAttribute()
                    && reader.Name == stringXmlNil
                    && reader.ReadContentAsBoolean())
                {
                    reader.MoveToElement();
                    value = null;
                    return;
                }

                reader.MoveToElement();
            }
        }
        else if (reader.ReadContentAsString() is { } stringValue
            && stringValue == stringXmlNil)
        {
            value = null;
            return;
        }
        T val = value ?? default;
        ValueSerializer.Read(reader, ref val);
        value = val;
    }

    public override void Write(XmlInvokeWriter writer, scoped ref T? value)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        T val = value.Value;
        ValueSerializer.Write(writer, ref val);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref T? value, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            value = null;
            return;
        }
        T val = value ?? default;
        ValueSerializer.Read(ref reader, ref val, options);
        value = val;
    }

    public override void Write(Utf8JsonWriter writer, scoped ref T? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        T val = value.Value;
        ValueSerializer.Write(writer, ref val, options);
    }

    public override void Read(ref MemoryPackReader reader, scoped ref T? value)
    {
        if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            reader.DangerousReadUnmanaged(out value);
            return;
        }
        if (!reader.TryReadObjectHeader(out var count))
        {
            value = null;
            return;
        }
        T val = value ?? default;
        ValueSerializer.Read(ref reader, ref val);
        value = val;
    }

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T? value)
    {
        if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            writer.DangerousWriteUnmanaged(value);
            return;
        }
        if (value == null)
        {
            writer.WriteNullObjectHeader();
            return;
        }
        writer.WriteObjectHeader(1);
        T val = value.Value;
        ValueSerializer.Write(ref writer, ref val);
    }

    public override void Read(ref MessagePackReader reader, scoped ref T? value, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            value = null;
            return;
        }

        T val = value ?? default;
        ValueSerializer.Read(ref reader, ref val, options);
        value = val;
    }

    public override void Write(ref MessagePackWriter writer, scoped ref T? value, MessagePackSerializerOptions options)
    {
        if (value is not T val)
        {
            writer.WriteNil();
            return;
        }

        ValueSerializer.Write(ref writer, ref val, options);
    }
}
