using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DataWF;

public static partial class TranslitExtensions
{
    private static readonly Dictionary<string, string> words = new(StringComparer.Ordinal)
    {
        { "Қс", "Ks" },
        { "ҚС", "KS" },
        { "қс", "ks" },
        { "Қ", "K" },
        { "Ң", "Ng" },
        { "Ө", "O" },
        { "Ұ", "U" },
        { "І", "I" },
        { "Ү", "U" },
        { "Ә", "A" },
        { "Ғ", "G" },
        { "Һ", "H" },

        { "қ", "k" },
        { "ң", "ng" },
        { "ө", "o" },
        { "ұ", "u" },
        { "і", "i" },
        { "ү", "u" },
        { "ә", "a" },
        { "ғ", "g" },
        { "һ", "h" },

        { "ье", "ye" },
        { "Кс", "X" },
        { "КС", "X" },
        { "кс", "x" },
        { "ДЖ", "J" },
        { "Дж", "J" },
        { "дж", "j" },
        { "а", "a" },
        { "б", "b" },
        { "в", "v" },
        { "г", "g" },
        { "д", "d" },
        { "е", "e" },
        { "ё", "yo" },
        { "ж", "zh" },
        { "з", "z" },
        { "и", "i" },
        { "й", "i" },
        { "к", "k" },
        { "л", "l" },
        { "м", "m" },
        { "н", "n" },
        { "о", "o" },
        { "п", "p" },
        { "р", "r" },
        { "с", "s" },
        { "т", "t" },
        { "у", "u" },
        { "ф", "f" },
        { "х", "kh" },
        { "ц", "ts" },
        { "ч", "ch" },
        { "ш", "sh" },
        { "щ", "chsh" },
        { "ъ", "" },
        { "ы", "y" },
        { "ь", "" },
        { "э", "e" },
        { "ю", "yu" },
        { "я", "ya" },
        { "А", "A" },
        { "Б", "B" },
        { "В", "V" },
        { "Г", "G" },
        { "Д", "D" },
        { "Е", "E" },
        { "Ё", "Yo" },
        { "Ж", "Zh" },
        { "З", "Z" },
        { "И", "I" },
        { "Й", "I" },
        { "К", "K" },
        { "Л", "L" },
        { "М", "M" },
        { "Н", "N" },
        { "О", "O" },
        { "П", "P" },
        { "Р", "R" },
        { "С", "S" },
        { "Т", "T" },
        { "У", "U" },
        { "Ф", "F" },
        { "Х", "Kh" },
        { "Ц", "Ts" },
        { "Ч", "Ch" },
        { "Ш", "Sh" },
        { "Щ", "Chsh" },
        { "Ъ", "" },
        { "Ы", "Y" },
        { "Ь", "" },
        { "Э", "E" },
        { "Ю", "Yu" },
        { "Я", "Ya" },
    };

    //http://devoid.com.ua/csharp/win-forms/transliter-na-c-sharp.html
    public static string Translit(this string p)
    {
        if (p == null)
            return null;
        string rez = p;
        rez = Regex.Replace(rez, @"й\b", "y");
        rez = Regex.Replace(rez, @"Й\b", "Y");
        rez = Regex.Replace(rez, @"\bЕ", "Ye");
        rez = Regex.Replace(rez, @"\bе", "ye");
        rez = Regex.Replace(rez, @"([ЙйУуЕеЫыАаОоЭэЯяИиЮю])с([ЙйУуЕеЫыАаОоЭэЯяИиЮю])", "$1ss$2");
        rez = Regex.Replace(rez, @"([ЙйУуЕеЫыАаОоЭэЯяИиЮю])С([ЙйУуЕеЫыАаОоЭэЯяИиЮю])", "$1SS$2");
        rez = Regex.Replace(rez, @"([ЙйУуЕеЫыАаОоЭэЯяИиЮю])е", "$1ye");
        rez = Regex.Replace(rez, @"([ЙйУуЕеЫыАаОоЭэЯяИиЮю])Е", "$1YE");

        foreach (KeyValuePair<string, string> pair in words)
        {
            rez = rez.Replace(pair.Key, pair.Value);
        }
        return rez;
    }


}

