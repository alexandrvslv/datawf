using Portable.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace DataWF.Common
{
    //https://github.com/cwensley/Portable.Xaml/blob/74d570bf7f75ab0c2fcdf4217e3019b3f24920e6/src/Portable.Xaml/Portable.Xaml.Markup/ValueSerializer.cs
    public class StringValueSerializer : ValueSerializer
    {
        public static readonly StringValueSerializer Instance = new StringValueSerializer();

        public override bool CanConvertFromString(string value, IValueSerializerContext context) => true;

        public override bool CanConvertToString(object value, IValueSerializerContext context) => true;

        public override object ConvertFromString(string value, IValueSerializerContext context)
        {
            return value;
        }

        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            return (string)value;
        }

        public override IEnumerable<Type> TypeReferences(object value, IValueSerializerContext context)
        {
            yield break;
        }
    }

    public class DateTimeValueSerializer : ValueSerializer
    {
        public static readonly DateTimeValueSerializer Instance = new DateTimeValueSerializer();

        public override bool CanConvertFromString(string value, IValueSerializerContext context) => true;

        public override bool CanConvertToString(object value, IValueSerializerContext context) => true;

        public override object ConvertFromString(string value, IValueSerializerContext context)
        {
            return DateTime.FromBinary(long.Parse(value));
        }

        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            return ((DateTime)value).ToBinary().ToString();
        }

        public override IEnumerable<Type> TypeReferences(object value, IValueSerializerContext context)
        {
            yield break;
        }
    }

    public class TypeValueSerializer : ValueSerializer
    {
        public static readonly TypeValueSerializer Instance = new TypeValueSerializer();

        public override bool CanConvertFromString(string value, IValueSerializerContext context) => true;

        public override bool CanConvertToString(object value, IValueSerializerContext context) => true;

        public override object ConvertFromString(string value, IValueSerializerContext context) => TypeHelper.ParseType(value);

        public override string ConvertToString(object value, IValueSerializerContext context) => TypeHelper.FormatBinary((Type)value);

        public override IEnumerable<Type> TypeReferences(object value, IValueSerializerContext context) { yield break; }
    }

    public class CultureInfoValueSerializer : ValueSerializer
    {
        public static readonly CultureInfoValueSerializer Instance = new CultureInfoValueSerializer();

        public override bool CanConvertFromString(string value, IValueSerializerContext context) => true;

        public override bool CanConvertToString(object value, IValueSerializerContext context) => true;

        public override object ConvertFromString(string value, IValueSerializerContext context) => CultureInfo.GetCultureInfo(value);

        public override string ConvertToString(object value, IValueSerializerContext context) => ((CultureInfo)value).Name;

        public override IEnumerable<Type> TypeReferences(object value, IValueSerializerContext context) { yield break; }
    }

    public class IntValueSerializer : ValueSerializer
    {
        public static readonly IntValueSerializer Instance = new IntValueSerializer();

        public override bool CanConvertFromString(string value, IValueSerializerContext context) => true;

        public override bool CanConvertToString(object value, IValueSerializerContext context) => true;

        public override object ConvertFromString(string value, IValueSerializerContext context) => int.TryParse(value, out var result) ? result : 0;

        public override string ConvertToString(object value, IValueSerializerContext context) => value.ToString();

        public override IEnumerable<Type> TypeReferences(object value, IValueSerializerContext context) { yield break; }
    }

    public class EnumValueSerializer<T> : ValueSerializer where T : struct
    {
        public override bool CanConvertFromString(string value, IValueSerializerContext context) => true;

        public override bool CanConvertToString(object value, IValueSerializerContext context) => true;

        public override object ConvertFromString(string value, IValueSerializerContext context) => Enum.TryParse<T>(value, out var result) ? result : default(T);

        public override string ConvertToString(object value, IValueSerializerContext context) => value.ToString();

        public override IEnumerable<Type> TypeReferences(object value, IValueSerializerContext context) { yield break; }
    }

    public class TypeConverterValueSerializer : ValueSerializer
    {
        public TypeConverter Converter { get; set; }

        public override bool CanConvertFromString(string value, IValueSerializerContext context) => Converter.CanConvertFrom(typeof(string));

        public override bool CanConvertToString(object value, IValueSerializerContext context) => Converter.CanConvertTo(typeof(string));

        public override object ConvertFromString(string value, IValueSerializerContext context) => Converter.ConvertFromInvariantString(value);

        public override string ConvertToString(object value, IValueSerializerContext context) => Converter.ConvertToInvariantString(value);

        public override IEnumerable<Type> TypeReferences(object value, IValueSerializerContext context) { yield break; }
    }
}
