using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DataWF.Common
{
    public interface INotifyListPropertyChanged : INotifyCollectionChanged, INotifyPropertyChanged, IList
    {
        IEnumerable<IFilterable> Views { get; }
        event PropertyChangedEventHandler ItemPropertyChanged;
        void OnListChanged(NotifyCollectionChangedAction type, object item = null, int index = -1, int oldIndex = -1, object oldItem = null);
        void OnItemPropertyChanged(object sender, PropertyChangedEventArgs args);
    }

    public interface IEntryNotifyPropertyChanged : INotifyPropertyChanged
    {
        IEnumerable<INotifyListPropertyChanged> Containers { get; }
    }

}
