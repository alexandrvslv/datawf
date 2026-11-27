using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using DataWF.Invokers.Defaults;
using DataWF.Serialization.Elements;
using MemoryPack;
using MessagePack;

namespace DataWF.Serialization;

public sealed class StringSerializer : ElementSerializer<string?>
{
    private const int MaxStackAllock = 512;
    public static readonly StringSerializer Instance = new();

    public static new ISerializeTypeInfo<string?> GetTypeInfo()
    {
        return new SerializeTypeInfo<string?>(
            keys: SerializeTypeKeys.Attribute,
            typeName: nameof(String),
            shortName: nameof(String),
            constructor: StringInvokers.StringConstructorCharArray.Instance,
            properties:
            [
                new SerializePropertyInfo<string?, int>(
                    invoker: StringInvokers.LengthInvoker.Instance!,
                    keys: SerializePropertyKeys.ReadOnlyNonSerializeAttribute,
                    serializer: Int32Serializer.Instance)
            ]
        );
    }

    public static string ReadString(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        if (length > 0)
        {
            if (length < MaxStackAllock)
            {
                Span<byte> bytes = stackalloc byte[length];
                reader.Read(bytes);
                return Encoding.UTF8.GetString(bytes);
            }
            else
                return Encoding.UTF8.GetString(reader.ReadBytes(length));
        }

        return string.Empty;
    }

    public static void WriteString(BinaryWriter writer, string? value)
    {
        var stringLength = value?.Length ?? 0;
        if (stringLength > 0)
        {
            var bytesLength = Encoding.UTF8.GetByteCount(value!);
            if (bytesLength < MaxStackAllock)
            {
                Span<byte> bytes = stackalloc byte[bytesLength];
                Encoding.UTF8.GetBytes(value.AsSpan(), bytes);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            else
            {
                var bytes = Encoding.UTF8.GetBytes(value!);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }
        else
        {
            writer.Write(0);
        }
    }

    public StringSerializer()
        : base(GetTypeInfo())
    { }

    public override bool IsAttributeSerializer => true;

    public override string? FromString(ReadOnlySpan<char> value)
        => value.ToString();

    public override string ToString(string? value)
        => value ?? string.Empty;

    public override string FormatQuery(string? value, string? format = null)
        => value == null ? stringNull : $"'{value.Replace("'", "''")}'";

    public override string FormatDisplay(string? value, string? format = null)
        => value ?? string.Empty;

    public override string? Parse(object value)
    {
        if (value is string stringValue)
            return stringValue;

        if (value == null || value == DBNull.Value)
            return null;

        if (SerializeProvider.GetSerializer(value.GetType()) is { } serializer
            && serializer.IsAttributeSerializer)
            return serializer.ToString(value);
        else if (value is IFormattable formattable)
            return formattable.ToString(string.Empty, CultureInfo.InvariantCulture);
        else if (TypeHelper.GetTypeConverter(value.GetType()) is { } typeConverter
                 && typeConverter.CanConvertTo(typeof(string)))
            return (string?)typeConverter.ConvertTo(value, typeof(string));
        else
            return value.ToString();
    }

    public override void Read(XmlInvokeReader reader, scoped ref string? value)
        => value = reader.ReadContentAsString() is { } content && content != stringXmlNil ? content : null;

    public override void Write(XmlInvokeWriter writer, scoped ref string? value)
        => writer.WriteValue(value ?? stringXmlNil);

    public override void Read(ref Utf8JsonReader reader, scoped ref string? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();

    public override void Write(Utf8JsonWriter writer, scoped ref string? value, JsonSerializerOptions options)
    {
        if (value is not null)
            writer.WriteStringValue(value);
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref string? value, JsonSerializerOptions options,
        ISerializePropertyInfo propertyInfo)
    {
        if (value is string notNullValue)
            writer.WriteString(propertyInfo.JsonName, notNullValue);
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override void Read(ref MemoryPackReader reader, scoped ref string? value)
        => value = reader.ReadString();

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref string? value)
        => writer.WriteString(value);

    public override void Read(ref MessagePackReader reader, scoped ref string? value, MessagePackSerializerOptions options)
        => value = reader.ReadString();

    public override void Write(ref MessagePackWriter writer, scoped ref string? value, MessagePackSerializerOptions options)
        => writer.Write(value);
}