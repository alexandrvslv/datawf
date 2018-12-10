using System.Collections;

namespace DataWF.Common
{
    public interface IModelView
    {
        ICollection Items { get; set; }
        object SelectedItem { get; set; }
    }
}