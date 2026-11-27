using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using DataWF.Collections;

namespace DataWF;

public class EnumItemList<T> : NamedList<EnumItem>
    where T : struct, Enum
{
    private T? itemValue;

    public EnumItemList()
        : base(EnumItem.NameInvoker.Instance)
    {
        Indexes.Add(EnumItem.ValueInvoker.Instance);
    }

    public EnumItemList(bool flags)
        : this(flags, EnumItem.GetEnumItems<T>())
    { }

    public EnumItemList(bool flags, IEnumerable<EnumItem> items)
        : this()
    {
        Flags = flags;
        AddRangeInternal(items, false);
    }

    public object Value
    {
        get => TypedValue;
        set => TypedValue = (T)value;
    }

    public T? TypedValue
    {
        get { return itemValue; }
        set
        {
            if (EqualityComparer<T?>.Default.Equals(itemValue, value))
                return;
            foreach (EnumItem<T> item in this)
            {
                item.Check = value == null
                    ? false
                    : item.TypedValue.HasFlag(value.Value);
            }
            itemValue = value;
            OnPropertyChanged();
        }
    }

    public IEnumerable<object> Values
    {
        get
        {
            foreach (var item in this)
            {
                if (item.Check)
                {
                    yield return item.Value;
                }
            }
        }
    }

    public Type Type => typeof(T);

    public bool Flags { get; }

    public override void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        base.OnItemPropertyChanged(sender, e);
        if (string.Equals(e.PropertyName, nameof(EnumItem.Check), StringComparison.Ordinal))
        {
            int temp = 0;
            foreach (var item in this)
            {
                if (item.Check)
                {
                    temp |= (int)item.Value;
                }
            }
            itemValue = (T?)Enum.ToObject(Type, temp);
            OnPropertyChanged(nameof(Value));
        }
    }

    public EnumItem? GetItem(object item)
        => item == null ? null : (EnumItem)GetItem((T)item);

    public EnumItem<T> GetItem(T item)
    {
        var enumItem = SelectOne(nameof(EnumItem.Value), item);
        if (enumItem == null)
        {
            enumItem = new EnumItem<T>(item);
            Add(enumItem);
        }
        return (EnumItem<T>)enumItem;
    }

    //public override int IndexOf(object item)
    //{
    //    if (item == null)
    //        return -1;
    //    if (item.GetType() == Type)
    //    {
    //        item = SelectOne(ValueInvoker.Name, item);
    //    }
    //    return IndexOf((EnumItem)item);
    //}

}


