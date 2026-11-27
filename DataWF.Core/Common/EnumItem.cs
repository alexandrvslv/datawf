using DataWF.Collections;
using DataWF.Invokers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace DataWF;

[Invoker]
public abstract partial class EnumItem : ICheck, INamed, INotifyPropertyChanged
{
    public static Dictionary<Type, INamedList<EnumItem>> Cache = new();

    public static IEnumerable<EnumItem<T>> GetEnumItems<T>(bool flags = false)
        where T : struct, Enum
    {
        int index = 0;
        foreach (T enumItem in Enum.GetValues<T>())
        {
            yield return new EnumItem<T>(enumItem, flags) { Index = index };
            index++;
        }
    }

    public static EnumItemList<T> GetList<T>(bool flags)
        where T : struct, Enum
    {
        var type = typeof(T);
        if (!Cache.TryGetValue(type, out var list))
        {
            Cache[type] = list = new EnumItemList<T>(flags, GetEnumItems<T>(flags));
        }

        return (EnumItemList<T>)list;
    }

    public static EnumItem<T> GetItem<T>(T item, bool flags) where T : struct, Enum
    {
        var list = GetList<T>(flags);
        var enumItem = list.GetItem(item);
        return enumItem;
    }

    public static string FormatUI<T>(T item, bool flags = false)
        where T : struct, Enum
    {
        EnumItem<T> enumItem = GetItem(item, flags);
        return enumItem.TextUI;
    }

    public static string Format<T>(T item, bool flags = false)
        where T : struct, Enum
    {
        EnumItem<T> enumItem = GetItem(item, flags);
        return enumItem.Text;
    }

    internal static string FormatInternal<T>(T item, bool flags = false)
        where T : struct, Enum
    {
        var name = item.ToString();
        var values = Enum.GetValues<T>();
        if (values.Contains(item))
        {
            return name;
        }
        else if (flags)
        {
            return string.Join(", ", values
                .Where(p => item.HasFlag(p))
                .Select(p => FormatInternal(p)));
        }

        return name;
    }

    public static object Parse(Type type, string value)
    {
        try
        {
            return Enum.Parse(type, value);
        }
        catch
        {
            var itemType = (typeof(EnumItem<>)).MakeGenericType(type);
            return InvokeProvider.Invoke(itemType, nameof(Parse), null, value);
        }
    }

    //public static bool operator ==(object item, EnumItem enumItem) { return item.Equals(enumItem.Value); }

    //public static bool operator !=(object item, EnumItem enumItem) { return !item.Equals(enumItem.Value); }


    private bool check;

    public int Index { get; set; }

    public abstract object Value { get; }

    public string Name { get; set; }

    public string Text { get; set; }

    public string TextUI { get; set; }

    public bool Check
    {
        get => check;
        set
        {
            if (check != value)
            {
                check = value;
                OnPropertyChanged(nameof(Check));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string property)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }

    public override string ToString() => Text;
}

[Invoker(Instance = true)]
public sealed class EnumItem<T> : EnumItem where T : struct, Enum
{
    public static string Format(T item)
    {
        return ((EnumItem<T>)item).Text;
    }

    public static explicit operator T(EnumItem<T> item) => item.TypedValue;

    public static explicit operator EnumItem<T>(T item) => new(item);

    public EnumItem()
    { }

    public EnumItem(T item, bool flags = false)
        : this(item, item.ToString(), FormatInternal(item, flags))
    { }

    public EnumItem(T item, string name, string text)
    {
        TypedValue = item;
        Name = name;
        Text = text;
        TextUI = Locale.Get(item.GetType(), text);
    }

    public T TypedValue { get; }

    public override object Value => TypedValue;

    public override int GetHashCode() => Value.GetHashCode();

    public override bool Equals(object? obj)
    {
        if (obj is EnumItem<T> item)
        {
            return Equals(item);
        }
        if (obj is T value)
        {
            return Equals(value);
        }
        return false;
    }

    public bool Equals(EnumItem<T>? obj)
    {
        if (obj == null)
            return false;
        return Equals(obj.TypedValue);
    }

    public bool Equals(T? obj) => EqualityComparer<T?>.Default.Equals(TypedValue, obj);


}

