using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Common
{
    public abstract class PropertyChangedDetailEventArgs : PropertyChangedEventArgs
    {
        public PropertyChangedDetailEventArgs(string propertyName)
            : base(propertyName)
        { }

        public abstract object OldObjectValue { get; }

        public abstract object NewObjectValue { get; }
    }

    public class PropertyChangedDetailEventArgs<T> : PropertyChangedDetailEventArgs
    {
        public PropertyChangedDetailEventArgs(string propertyName, T oldValue, T newValue)
            : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public T OldValue { get; }

        public T NewValue { get; }

        public override object OldObjectValue => OldValue;

        public override object NewObjectValue => NewValue;
    }

    public class PropertyChangedAggregateEventArgs : PropertyChangedEventArgs
    {
        public PropertyChangedAggregateEventArgs(PropertyChangedEventArgs firstProperty) : this(firstProperty.PropertyName)
        {
            Items.Add(firstProperty);
        }

        public PropertyChangedAggregateEventArgs(string firstProperty) : base(firstProperty)
        {
        }

        public List<PropertyChangedEventArgs> Items { get; } = new List<PropertyChangedEventArgs>();
    }
}