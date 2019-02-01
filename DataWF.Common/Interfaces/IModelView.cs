using System.Collections;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IModelView
    {
        ICollection Items { get; set; }
        object SelectedItem { get; set; }
        Task Save(object item);
    }
}