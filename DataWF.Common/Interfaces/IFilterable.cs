using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DataWF.Common
{
    public interface IFilterable : INotifyListPropertyChanged
    {
        IEnumerable Source { get; set; }

        IQuery FilterQuery { get; set; }

        event EventHandler FilterChanged;

        void UpdateFilter();

        void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e);
        void OnSourceItemChanged(object sender, PropertyChangedEventArgs e);
    }

    public interface IFilterable<T> : IFilterable, IList<T>
    {
        new Query<T> FilterQuery { get; set; }

    }
}
