using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IModelView : INotifyValidation, INotifyPropertyChanged
    {
        Type ItemType { get; }
        IWebClient WebClient { get; }
        IFilterable Items { get; set; }
        IQuery Filter { get; set; }
        object SelectedItem { get; set; }
        object HoveredItem { get; set; }
        IEnumerable<object> SelectedItems { get; set; }
        bool VirtualMode { get; set; }
        bool PreventLoad { get; set; }
        bool HideArchive { get; set; }
        bool AllowStatus { get; }
        ModelViewStatus Status { get; }

        event EventHandler<ModelViewEventArgs> SelectedItemChanged;
        event EventHandler<ModelViewEventArgs> SelectedItemsChanged;
        event EventHandler<ModelViewEventArgs> Added;
        event EventHandler<ModelViewEventArgs> Removed;

        event EventHandler StatusChanged;
        Task<IEnumerable> Get(HttpPageSettings pages = null);
        bool CanEdit(object item);
        Task Save(object item);
        Task Save(object item, ProgressToken token);
        void ChangeCanExecute();
        IEnumerable<object> SearchByValue(IInvoker valueInvoker, object value);
    }

    public interface IModelView<T> : IModelView
    {
        new T SelectedItem { get; set; }
        new IFilterable<T> Items { get; set; }
        new Query<T> Filter { get; set; }

        Task<IEnumerable<T>> Get(string filter, HttpPageSettings list = null);
    }

    public interface ICrudModelView<T> : IModelView<T>
    {
        bool AccessCreate { get; }
        bool AccessRead { get; }
        bool AccessUpdate { get; }
        bool AccessAdmin { get; }

        Task<IEnumerable<T>> GetInternal(string filter, HttpPageSettings list = null);
    }
}