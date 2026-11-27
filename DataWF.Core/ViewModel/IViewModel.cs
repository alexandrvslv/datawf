using DataWF.Collections;
using DataWF.Collections.Query;
using DataWF.Invokers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace DataWF.ModelView
{
    public interface IViewModel : INotifyValidation, INotifyPropertyChanged
    {
        Type ItemType { get; }
        IWebClient WebClient { get; }
        IFilterable Items { get; set; }
        IQQuery Filter { get; set; }
        object SelectedItem { get; set; }
        object HoveredItem { get; set; }
        IEnumerable<object> SelectedItems { get; set; }
        bool VirtualMode { get; set; }
        bool PreventLoad { get; set; }
        bool HideArchive { get; set; }
        bool AllowStatus { get; }
        ViewModelStatus Status { get; }

        event EventHandler<ViewModelEventArgs> SelectedItemChanged;
        event EventHandler<ViewModelEventArgs> SelectedItemsChanged;
        event EventHandler<ViewModelEventArgs> Added;
        event EventHandler<ViewModelEventArgs> Removed;

        event EventHandler StatusChanged;
        Task<IEnumerable> Get(HttpPageSettings pages = null);
        bool CanEdit(object item);
        Task Save(object item);
        Task Save(object item, ProgressToken token);
        void ChangeCanExecute();
        IEnumerable<object> SearchByValue(IInvoker valueInvoker, object value);
    }

    public interface IViewModel<T> : IViewModel
    {
        new T SelectedItem { get; set; }
        new IFilterable<T> Items { get; set; }
        new QQuery<T> Filter { get; set; }

        Task<IEnumerable<T>> Get(string filter, HttpPageSettings list = null);
    }

    public interface ICrudViewModel<T> : IViewModel<T>
    {
        bool AccessCreate { get; }
        bool AccessRead { get; }
        bool AccessUpdate { get; }
        bool AccessAdmin { get; }

        Task<IEnumerable<T>> GetInternal(string filter, HttpPageSettings list = null);
    }
}