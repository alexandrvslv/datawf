using DataWF.Collections.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DataWF.Collections;

public interface IFilterable : INotifyListPropertyChanged
{
    IEnumerable Source { get; set; }

    IQQuery FilterQuery { get; set; }

    event EventHandler FilterChanged;

    void UpdateFilter();

    void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e);
    void OnSourceItemChanged(object sender, PropertyChangedEventArgs e);
}

public interface IFilterable<T> : IFilterable, IList<T>
{
    new IQQuery<T> FilterQuery { get; set; }

    IQQuery IFilterable.FilterQuery
    {
        get => FilterQuery;
        set => FilterQuery = (IQQuery<T>)value;
    }

}
