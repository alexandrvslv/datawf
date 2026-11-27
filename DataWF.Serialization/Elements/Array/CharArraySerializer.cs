using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Linq;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class CharArraySerializer : UnmanagedArraySerializer<char>
{
    public static readonly CharArraySerializer Instance = new();

    public override bool IsAttributeSerializer => true;

    public override char[]? FromString(ReadOnlySpan<char> value)
        => value.IsEmpty ? Array.Empty<char>() : value.ToArray();

    public override string ToString(char[]? value)
        => value == null ? string.Empty : new string(value);

    public override string FormatDisplay(char[]? value, string? format = null)
    {
        return value == null ? stringNull : $"\"{value}\"";
    }

    public override char[] Parse(object value)
    {
        return value switch
        {
            char[] typedValue => typedValue,
            string strValue => strValue.ToCharArray(),
            IEnumerable<char> charEnumerable => charEnumerable.ToArray(),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType()} to char[].")
        };
    }

    public override void Read(XmlInvokeReader reader, scoped ref char[]? value)
        => value = ReadChars(reader);

    public override void Write(XmlInvokeWriter writer, scoped ref char[]? value)
    {
        if (value is char[] notNullValue)
            writer.WriteChars(notNullValue);
        else
            writer.WriteNullValue();
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref char[]? value, JsonSerializerOptions options)
        => value = ReadChars(ref reader);

    public override void Write(Utf8JsonWriter writer, scoped ref char[]? value, JsonSerializerOptions options)
    {
        if (value is char[] notNullValue)
            writer.WriteStringValue(notNullValue);
        else
            writer.WriteNullValue();
    }

    public override void Read(ref MessagePackReader reader, scoped ref char[]? value, MessagePackSerializerOptions options)
    {
        value = MessagePack.Formatters.CharArrayFormatter.Instance.Deserialize(ref reader, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref char[]? value, MessagePackSerializerOptions options)
    {
        MessagePack.Formatters.CharArrayFormatter.Instance.Serialize(ref writer, value, options);
    }

    public static char[]? ReadChars(XmlInvokeReader reader)
    {
        char[] rentBuffer = ArrayPool<char>.Shared.Rent(4096);
        
        try
        {
            int read = reader.ReadValueChunk(rentBuffer, 0, 8);
            
            if (read == 0) return Array.Empty<char>();

            if (read == 3 && rentBuffer.AsSpan(0, 3).SequenceEqual(stringXmlNil))
            {
                return null;
            }

            if (read < 8)
            {
                var shortResult = new char[read];
                Array.Copy(rentBuffer, 0, shortResult, 0, read);
                return shortResult;
            }

            int currentLength = read;
            int bufferSize = rentBuffer.Length;

            while ((read = reader.ReadValueChunk(rentBuffer, currentLength, bufferSize - currentLength)) > 0)
            {
                currentLength += read;

                if (currentLength == bufferSize)
                {
                    char[] newBuffer = ArrayPool<char>.Shared.Rent(bufferSize * 2);
                    Array.Copy(rentBuffer, 0, newBuffer, 0, currentLength);
                    ArrayPool<char>.Shared.Return(rentBuffer);
                    rentBuffer = newBuffer;
                    bufferSize = rentBuffer.Length;
                }
            }

            var finalResult = new char[currentLength];
            Array.Copy(rentBuffer, 0, finalResult, 0, currentLength);
            return finalResult;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rentBuffer);
        }
    }

    public static char[]? ReadChars(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (!reader.HasValueSequence)
        {
            ReadOnlySpan<byte> utf8Span = reader.ValueSpan;
            int charCount = Encoding.UTF8.GetCharCount(utf8Span);
            var charBuffer = new char[charCount];
            Encoding.UTF8.GetChars(utf8Span, charBuffer);
            return charBuffer;
        }
        else
        {
            int length = GetCharCount(reader.ValueSequence, Encoding.UTF8);
            char[] charBuffer = new char[length];
            try
            {
                int charsRead = reader.CopyString(charBuffer);
                return charBuffer;
            }
            catch
            {
                return null;
            }
        }
    }

    public static int GetCharCount(ReadOnlySequence<byte> sequence, Encoding encoding)
    {
        int charCount = 0;
        foreach (var segment in sequence)
        {
            charCount += encoding.GetCharCount(segment.Span);
        }
        return charCount;
    }    

}
