using System;
using System.Collections;
using System.ComponentModel;

namespace DataWF.Common
{
    public interface INotifyListChanged : IList
    {
        event ListChangedEventHandler ListChanged;

        void OnListChanged(ListChangedType type, int oldindex = -1, int newindex = -1, string property = null);

        void OnPropertyChanged(object sender, PropertyChangedEventArgs e);
    }

    public interface IContainerNotifyPropertyChanged : INotifyPropertyChanged
    {
        INotifyListChanged Container { get; set; }
    }
}
