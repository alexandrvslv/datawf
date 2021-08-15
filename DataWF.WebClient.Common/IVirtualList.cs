using DataWF.Common;
using System.Collections;
using System.Threading.Tasks;

namespace DataWF.WebClient.Common
{
    public interface IVirtualList : IList
    {
        IModelView ModelView { get; set; }

        int PageSize { get; set; }

        ValueTask<object> GetItemAsync(int index);
    }
}