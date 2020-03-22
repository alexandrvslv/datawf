using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Common
{
    public class PropertyChangedDetailEventArgs : PropertyChangedEventArgs
    {
        public PropertyChangedDetailEventArgs(string propertyName, object oldValue, object newValue)
            : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public object OldValue { get; }

        public object NewValue { get; }
    }

    public class PropertyChangedAggregateEventArgs : PropertyChangedEventArgs
    {
        public PropertyChangedAggregateEventArgs(string firstProperty) : base(firstProperty)
        {
        }

        public List<PropertyChangedEventArgs> Items { get; } = new List<PropertyChangedEventArgs>();
    }
}