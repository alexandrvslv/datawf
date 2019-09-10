using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IModelView : INotifyValidation
    {
        ICollection Items { get; set; }
        object SelectedItem { get; set; }
        IEnumerable<object> SelectedItems { get; set; }

        Task Save(object item);
        Task Save(object item, ProgressToken token);

    }
}