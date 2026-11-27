using System;
using System.Buffers;
using System.Data.Common;
using System.Text.Json;
using System.Xml;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class ByteArraySerializer : UnmanagedArraySerializer<byte>
{
    public static readonly ByteArraySerializer Instance = new();

    public override bool IsAttributeSerializer => true;

    public override byte[]? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetFieldValue<byte[]>(i);

    public override byte[]? FromString(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return null;
        int maxBytes = (value.Length * 3) / 4;
        byte[]? buffer = null;
        try
        {
            Span<byte> temp = maxBytes < 200 ? stackalloc byte[maxBytes] : ArrayPool<byte>.Shared.Rent(maxBytes);
            if (Convert.TryFromBase64Chars(value, temp, out int bytesWritten))
            {
                byte[] result = new byte[bytesWritten];
                temp.Slice(0, bytesWritten).CopyTo(result);
                return result;
            }
        }
        finally
        {
            if (buffer != null)
                ArrayPool<byte>.Shared.Return(buffer);
        }
        return null;
    }
    public override string ToString(byte[]? value)
        => value == null ? string.Empty : Convert.ToBase64String(value);

    public override void Read(XmlInvokeReader reader, ref byte[]? value)
        => value = reader.ReadContentAsString() is not { } stringValue || stringValue == stringXmlNil ? null
            : FromString(stringValue);

    public override void Write(XmlInvokeWriter writer, ref byte[]? value)
    {
        if (value != null)
            writer.WriteBase64(value, 0, value.Length);
        else
            writer.WriteNullValue();
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref byte[]? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? null : reader.GetBytesFromBase64();

    public override void Write(Utf8JsonWriter writer, ref byte[]? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteBase64StringValue(value);
    }

    override public void Write(Utf8JsonWriter writer, ref byte[]? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value == null)
            writer.WriteNull(propertyInfo.JsonName);
        else
            writer.WriteBase64String(propertyInfo.JsonName, value);
    }

    public override void Read(ref MessagePackReader reader, scoped ref byte[]? value, MessagePackSerializerOptions options)
    {
        value = MessagePack.Formatters.ByteArrayFormatter.Instance.Deserialize(ref reader, options);
    }

    public override void Write(ref MessagePackWriter writer, scoped ref byte[]? value, MessagePackSerializerOptions options)
    {
        writer.Write(value);
    }

    public override string FormatDisplay(byte[]? value, string? format = null)
        => value == null ? string.Empty : Convert.ToHexString(value);

    public override string FormatQuery(byte[]? value, string? format = null)
        => value == null ? stringNull : $"0x{Convert.ToHexString(value)}";

    public override byte[]? Parse(object? value)
    {
        if (value is byte[] exact)
            return exact;
        if (value == null)
            return null;
        if (value is string str)
        {
            return str.StartsWith("0x")
                ? Convert.FromHexString(str.AsSpan(2))
                : Convert.FromBase64String(str);
        }
        return null;
    }

    public static byte[]? GetByteArray(XmlInvokeReader reader)
    {
        byte[] rentBuffer = ArrayPool<byte>.Shared.Rent(4096);

        try
        {
            int currentLength = 0;
            int read;
            int bufferSize = rentBuffer.Length;

            // 2. Потоковое чтение Base64 напрямую в байтовый массив
            // Метод автоматически декодирует текстовый Base64 в массив байт chunks
            while ((read = reader.ReadContentAsBase64(rentBuffer, currentLength, bufferSize - currentLength)) > 0)
            {
                currentLength += read;

                // Если арендованный буфер заполнился, расширяем его
                if (currentLength == bufferSize)
                {
                    byte[] newBuffer = ArrayPool<byte>.Shared.Rent(bufferSize * 2);
                    Array.Copy(rentBuffer, 0, newBuffer, 0, currentLength);
                    ArrayPool<byte>.Shared.Return(rentBuffer);
                    rentBuffer = newBuffer;
                    bufferSize = rentBuffer.Length;
                }
            }

            if (currentLength == 0) return Array.Empty<byte>();

            // 3. Выделяем итоговый массив точного размера под результат
            byte[] finalResult = new byte[currentLength]; // В вашей логике возврат точного массива
            Array.Copy(rentBuffer, 0, finalResult, 0, currentLength);
            return finalResult;
        }
        catch
        {
            return null;
        }
        finally
        {
            // Обязательно возвращаем массив в пул
            ArrayPool<byte>.Shared.Return(rentBuffer);
        }

    }

    public static byte[]? GetByteArrayByChars(XmlInvokeReader reader)
    {
        char[] rentCharBuffer = ArrayPool<char>.Shared.Rent(4096);

        try
        {
            int charsRead = reader.ReadValueChunk(rentCharBuffer, 0, 8);

            if (charsRead == 0) return Array.Empty<byte>();

            if (charsRead == 3 && rentCharBuffer.AsSpan(0, 3).SequenceEqual(stringXmlNil))
            {
                return null;
            }

            int currentLength = charsRead;
            int bufferSize = rentCharBuffer.Length;

            while ((charsRead = reader.ReadValueChunk(rentCharBuffer, currentLength, bufferSize - currentLength)) > 0)
            {
                currentLength += charsRead;

                if (currentLength == bufferSize)
                {
                    char[] newBuffer = ArrayPool<char>.Shared.Rent(bufferSize * 2);
                    Array.Copy(rentCharBuffer, 0, newBuffer, 0, currentLength);
                    ArrayPool<char>.Shared.Return(rentCharBuffer);
                    rentCharBuffer = newBuffer;
                    bufferSize = rentCharBuffer.Length;
                }
            }

            int maxByteLength = (currentLength * 3) / 4;
            byte[] finalBytes = new byte[maxByteLength];

            if (Convert.TryFromBase64Chars(rentCharBuffer.AsSpan(0, currentLength), finalBytes, out int bytesWritten))
            {
                // Если массив оказался чуть больше из-за символов выравнивания (padding '='),
                // подрезаем его до точного размера прочитанных байт
                if (bytesWritten < finalBytes.Length)
                {
                    Array.Resize(ref finalBytes, bytesWritten);
                }
                return finalBytes;
            }

            throw new XmlException("Невалидный формат Base64 в XML-узле.");
        }
        finally
        {
            // Обязательно возвращаем текстовый буфер в пул
            ArrayPool<char>.Shared.Return(rentCharBuffer);
        }
    }
}
