using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

namespace DataWF.Serialization;

public sealed class Utf8StringOrdinalIgnoreCaseComparer : IEqualityComparer<string>,
    IAlternateEqualityComparer<ReadOnlySpan<byte>, string>
{
    // Универсальный сид рантайма .NET для детерминированного Marvin хэша.
    private const uint MarvinSeed = 0xDEADBEEF;
    public static Utf8StringOrdinalIgnoreCaseComparer Instance { get; } = new();

    private Utf8StringOrdinalIgnoreCaseComparer()
    {
    }

    public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);

    public string Create(ReadOnlySpan<byte> alternate) => Encoding.UTF8.GetString(alternate);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public bool Equals(ReadOnlySpan<byte> alternate, string other)
    {
        if (other == null) return false;

        if (alternate.Length < other.Length) return false;

        var utf16Span = other.AsSpan();

        if (Ascii.IsValid(alternate))
        {
            return Ascii.EqualsIgnoreCase(alternate, utf16Span);
        }

        var utf16Enumerator = other.EnumerateRunes();
        ReadOnlySpan<byte> utf8Remaining = alternate;

        while (utf16Enumerator.MoveNext())
        {
            if (utf8Remaining.IsEmpty) return false;

            // Декодируем одну руну с начала UTF-8 спана
            if (Rune.DecodeFromUtf8(utf8Remaining, out Rune utf8Rune, out int bytesConsumed) != OperationStatus.Done)
            {
                return false; // Невалидный UTF-8
            }

            Rune utf16Rune = utf16Enumerator.Current;

            if (utf16Rune != utf8Rune && Rune.ToUpperInvariant(utf16Rune) != Rune.ToUpperInvariant(utf8Rune))
            {
                return false;
            }

            // Сдвигаем окно просмотра вперед
            utf8Remaining = utf8Remaining[bytesConsumed..];
        }

        return utf8Remaining.IsEmpty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ReadOnlySpan<byte> alternate, ReadOnlySpan<byte> utf8Span)
    {
        if (alternate.SequenceEqual(utf8Span))
            return true;
        if (Ascii.IsValid(alternate))
        {
            return Ascii.EqualsIgnoreCase(alternate, utf8Span);
        }    
        
        ReadOnlySpan<byte> spanX = alternate;
        ReadOnlySpan<byte> spanY = utf8Span;
        while (!spanX.IsEmpty && !spanY.IsEmpty)
        {
            var statusX = Rune.DecodeFromUtf8(spanX, out var runeX, out var bytesConsumedX);
            var statusY = Rune.DecodeFromUtf8(spanY, out var runeY, out var bytesConsumedY);

            if ((statusX | statusY) != OperationStatus.Done)
            {
                return spanX.SequenceEqual(spanY);
            }

            if (Rune.ToUpperInvariant(runeX) != Rune.ToUpperInvariant(runeY))
            {
                return false;
            }

            spanX = spanX.Slice(bytesConsumedX);
            spanY = spanY.Slice(bytesConsumedY);
        }

        return spanX.IsEmpty && spanY.IsEmpty;
    }


    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public int GetHashCode(string obj)
    {
        if (obj == null) return 0;

        // 1. FAST-PATH ДЛЯ ASCII СТРОК
        if (TryComputeMarvinAscii(obj.AsSpan(), out var hash))
        {
            return hash;
        }

        // 2. UNICODE-ФОЛЛБЭК ДЛЯ СТРОК
        var unicodeHc = new HashCode();
        foreach (Rune rune in obj.EnumerateRunes())
        {
            unicodeHc.Add(Rune.ToUpperInvariant(rune).Value);
        }

        return unicodeHc.ToHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public int GetHashCode(ReadOnlySpan<byte> alternate)
    {
        // 1. FAST-PATH ДЛЯ ASCII UTF-8 БАЙТ (Самый частый кейс для JSON ключей)
        if (TryComputeMarvinAscii(alternate, out var hash))
        {
            return hash;
        }

        // 2. UNICODE-ФОЛЛБЭК ДЛЯ UTF-8 БАЙТ (Без аллокаций через Runes)
        var unicodeHc = new HashCode();
        ReadOnlySpan<byte> utf8Remaining = alternate;

        while (!utf8Remaining.IsEmpty)
        {
            if (Rune.DecodeFromUtf8(utf8Remaining, out Rune utf8Rune, out int bytesConsumed) != OperationStatus.Done)
            {
                // Защита от невалидного UTF-8
                unicodeHc.Add(utf8Remaining[0]);
                utf8Remaining = utf8Remaining[1..];
                continue;
            }

            unicodeHc.Add(Rune.ToUpperInvariant(utf8Rune).Value);
            utf8Remaining = utf8Remaining[bytesConsumed..];
        }

        return unicodeHc.ToHashCode();
    }

    // Векторизованный (по 4 байта) подсчет Marvin хэша с приведением регистра "на лету" из UTF-8 Span
    private static bool TryComputeMarvinAscii(ReadOnlySpan<byte> asciiData, out int hash)
    {
        int length = asciiData.Length;
        uint p0 = MarvinSeed;
        uint p1 = MarvinSeed;

        ref byte ptr = ref MemoryMarshal.GetReference(asciiData);
        int index = 0;

        while (length >= 4)
        {
            uint block = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref ptr, index));

            if ((block & 0x80808080u) != 0)
            {
                hash = 0;
                return false;
            }

            // Branchless ToLower для 4 байт одновременно (строго для 'A'-'Z')
            uint upperMask = block - 0x41414141u;
            uint lowerMask = block - 0x5B5B5B5Bu;
            uint isWithInRange = (upperMask ^ lowerMask) & 0x80808080u;
            uint toLowerMask = (isWithInRange >> 2) & 0x20202020u;
            block |= toLowerMask;

            // Раунд Marvin хэша
            p0 += block;
            p1 ^= p0;
            p0 = System.Numerics.BitOperations.RotateLeft(p0, 20);
            p0 += p1;
            p1 = System.Numerics.BitOperations.RotateLeft(p1, 9);
            p1 ^= p0;
            p0 = System.Numerics.BitOperations.RotateLeft(p0, 34);
            p0 += p1;
            p1 = System.Numerics.BitOperations.RotateLeft(p1, 13);

            index += 4;
            length -= 4;
        }

        uint finalBlock = 0x80;
        switch (length)
        {
            case 3:
                finalBlock = (finalBlock << 8) | ToLowerAscii(Unsafe.Add(ref ptr, index + 2));
                goto case 2;
            case 2:
                finalBlock = (finalBlock << 8) | ToLowerAscii(Unsafe.Add(ref ptr, index + 1));
                goto case 1;
            case 1: finalBlock = (finalBlock << 8) | ToLowerAscii(Unsafe.Add(ref ptr, index)); break;
        }

        p0 += finalBlock;
        p1 ^= p0;
        p0 = System.Numerics.BitOperations.RotateLeft(p0, 20);
        p0 += p1;
        p1 = System.Numerics.BitOperations.RotateLeft(p1, 9);
        p1 ^= p0;
        p0 = System.Numerics.BitOperations.RotateLeft(p0, 34);
        p0 += p1;
        p1 = System.Numerics.BitOperations.RotateLeft(p1, 13);

        p0 ^= p1;
        hash = (int)(p0 ^ (p0 >> 16));
        return true;
    }

    // Векторизованный подсчет Marvin хэша из UTF-16 ReadOnlySpan<char> (заменяет string.GetHashCode)
    private static bool TryComputeMarvinAscii(ReadOnlySpan<char> asciiStr, out int hash)
    {
        int length = asciiStr.Length;
        uint p0 = MarvinSeed;
        uint p1 = MarvinSeed;

        ref char ptr = ref MemoryMarshal.GetReference(asciiStr);
        int index = 0;

        while (length >= 4)
        {
            // Загружаем сразу 4 символа char (8 байт) в 64-битный регистр
            ulong rawQuadChars = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref ptr, index)));

            if ((rawQuadChars & 0xFF80FF80FF80FF80ul) != 0)
            {
                hash = 0;
                return false;
            }

            // Pack the low byte of each ASCII UTF-16 character into a UTF-8 byte block.
            uint block = (uint)(rawQuadChars & 0xFF)
                         | (uint)((rawQuadChars >> 8) & 0xFF00)
                         | (uint)((rawQuadChars >> 16) & 0xFF0000)
                         | (uint)((rawQuadChars >> 24) & 0xFF000000);

            // Тот же Branchless ToLower для совпадения стейта block
            uint upperMask = block - 0x41414141u;
            uint lowerMask = block - 0x5B5B5B5Bu;
            uint isWithInRange = (upperMask ^ lowerMask) & 0x80808080u;
            uint toLowerMask = (isWithInRange >> 2) & 0x20202020u;
            block |= toLowerMask;

            // Раунд Marvin хэша (идентичен байтивому)
            p0 += block;
            p1 ^= p0;
            p0 = System.Numerics.BitOperations.RotateLeft(p0, 20);
            p0 += p1;
            p1 = System.Numerics.BitOperations.RotateLeft(p1, 9);
            p1 ^= p0;
            p0 = System.Numerics.BitOperations.RotateLeft(p0, 34);
            p0 += p1;
            p1 = System.Numerics.BitOperations.RotateLeft(p1, 13);

            index += 4;
            length -= 4;
        }

        uint finalBlock = 0x80;
        switch (length)
        {
            case 3:
                finalBlock = (finalBlock << 8) | ToLowerAscii((byte)Unsafe.Add(ref ptr, index + 2));
                goto case 2;
            case 2:
                finalBlock = (finalBlock << 8) | ToLowerAscii((byte)Unsafe.Add(ref ptr, index + 1));
                goto case 1;
            case 1: finalBlock = (finalBlock << 8) | ToLowerAscii((byte)Unsafe.Add(ref ptr, index)); break;
        }

        p0 += finalBlock;
        p1 ^= p0;
        p0 = System.Numerics.BitOperations.RotateLeft(p0, 20);
        p0 += p1;
        p1 = System.Numerics.BitOperations.RotateLeft(p1, 9);
        p1 ^= p0;
        p0 = System.Numerics.BitOperations.RotateLeft(p0, 34);
        p0 += p1;
        p1 = System.Numerics.BitOperations.RotateLeft(p1, 13);

        p0 ^= p1;
        hash = (int)(p0 ^ (p0 >> 16));
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ToLowerAscii(byte b)
    {
        // Если символ в диапазоне 'A'-'Z', переводим в нижний регистр 'a'-'z'
        if (b >= 0x41 && b <= 0x5A)
        {
            return (byte)(b | 0x20);
        }

        return b;
    }
}