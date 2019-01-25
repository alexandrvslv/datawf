using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DataWF.Common
{
    public interface INotifyListPropertyChanged : INotifyCollectionChanged, INotifyPropertyChanged, IList
    {
        void OnListChanged(NotifyCollectionChangedAction type, object item = null, int index = -1, int oldIndex = -1, object oldItem = null);
        event PropertyChangedEventHandler ItemPropertyChanged;
        void OnItemPropertyChanged(object sender, PropertyChangedEventArgs args);
    }

    public interface IContainerNotifyPropertyChanged : INotifyPropertyChanged
    {
        IEnumerable<INotifyListPropertyChanged> Containers { get; }
    }

}
