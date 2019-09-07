using System.Collections;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IModelView : INotifyValidation
    {
        ICollection Items { get; set; }
        object SelectedItem { get; set; }
        Task Save(object item);
        Task Save(object item, ProgressToken token);

    }
}