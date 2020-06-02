using DataWF.WebClient.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IModelView : INotifyValidation
    {
        Type ItemType { get; }
        IFilterable Items { get; set; }
        IQuery Filter { get; set; }
        object SelectedItem { get; set; }
        IEnumerable<object> SelectedItems { get; set; }
        bool VirtualMode { get; set; }
        bool PreventLoad { get; set; }
        bool AllowStatus { get; }
        ModelViewStatus Status { get; }

        event EventHandler SelectedItemChanged;
        event EventHandler StatusChanged;
        Task Get(HttpPageSettings pages = null);
        bool CanEdit(object item);
        Task Save(object item);
        Task Save(object item, ProgressToken token);
    }

    public enum ModelViewStatus
    {
        None,
        Compleate,
        Loading,
        Saving,
        Deleting,
        Creating,
        Uploading,
        Downloading
    }
}