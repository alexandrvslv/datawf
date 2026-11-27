using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF;

public abstract class PropertyChangedDetailEventArgs : PropertyChangedEventArgs
{
    public PropertyChangedDetailEventArgs(string? propertyName)
        : base(propertyName)
    { }

    public abstract object? GetOldValue();

    public abstract object? GetNewValue();
}

public class PropertyChangedDetailEventArgs<T> : PropertyChangedDetailEventArgs
{
    public PropertyChangedDetailEventArgs(string? propertyName, T oldValue, T newValue)
        : base(propertyName)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public T OldValue { get; }

    public T NewValue { get; }

    public override object? GetOldValue() => OldValue;

    public override object? GetNewValue() => NewValue;
}

public class PropertyChangedAggregateEventArgs : PropertyChangedEventArgs
{
    public PropertyChangedAggregateEventArgs(PropertyChangedEventArgs firstProperty)
        : this(firstProperty.PropertyName)
    {
        Items.Add(firstProperty);
    }

    public PropertyChangedAggregateEventArgs(string? firstProperty)
        : base(firstProperty)
    {
    }

    public List<PropertyChangedEventArgs> Items { get; } = new();
}