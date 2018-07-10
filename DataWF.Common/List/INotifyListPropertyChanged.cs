using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DataWF.Common
{
    public interface INotifyListPropertyChanged : INotifyCollectionChanged, INotifyPropertyChanged, IList
    {
        void OnListChanged(NotifyCollectionChangedAction type, object item = null, int index = -1, string property = null, int oldIndex = -1, object oldItem = null);

        void OnPropertyChanged(object sender, PropertyChangedEventArgs e);
    }

    public interface IContainerNotifyPropertyChanged : INotifyPropertyChanged
    {
        INotifyListPropertyChanged Container { get; set; }
    }

}
