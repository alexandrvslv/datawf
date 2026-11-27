using DataWF.ModelView;
using System.Collections;
using System.Threading.Tasks;

namespace DataWF.Collections
{
    public interface IVirtualList : IList
    {
        IViewModel ModelView { get; set; }

        int PageSize { get; set; }

        ValueTask<object> GetItemAsync(int index);

        Task LoadAsync();
    }
}