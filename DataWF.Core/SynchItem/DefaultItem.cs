using DataWF.Collections;
using DataWF.Comparers;
using DataWF.Core.Helper;
using DataWF.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF;

public abstract class DefaultItem : IEntryNotifyPropertyChanged
{
    public static Action<PropertyChangedEventHandler, object, PropertyChangedEventArgs>? GlogalChangedHook;
    protected ArrayPointerList<PropertyChangedEventHandler>? propertyChanged;

    [ElementSerializer(Ignore = true), Browsable(false)]
    public IEnumerable<INotifyListPropertyChanged> Containers => propertyChanged.GetContainers<INotifyListPropertyChanged, PropertyChangedEventHandler>();

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            if (propertyChanged == null)
                propertyChanged = new ArrayPointerList<PropertyChangedEventHandler>(value);
            else
                propertyChanged.Add(value);
        }
        remove => propertyChanged?.Remove(value);
    }

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs arg)
    {
        foreach (PropertyChangedEventHandler handler in propertyChanged)
        {
            if (GlogalChangedHook == null
                || handler.Target is INotifyListPropertyChanged)
            {
                handler.Invoke(this, arg);
            }
            else
            {
                GlogalChangedHook(handler, this, arg);
            }
        }
    }

    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (propertyChanged != null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }

    protected virtual void ChangeProperty<T>(ref T value, T newValue, [CallerMemberName] string? propertyName = null)
    {
        if (UniversalComparer<T>.Default.Equals(value, newValue))
            return;
        var oldValue = value;
        value = newValue;
        OnPropertyChanged(oldValue, newValue, propertyName);
    }

    protected virtual void OnPropertyChanged<T>(T oldValue, T newValue, [CallerMemberName] string? propertyName = null)
    {
        if (propertyChanged != null)
        {
            OnPropertyChanged(new PropertyChangedDetailEventArgs<T>(propertyName, oldValue, newValue));
        }
    }
}
