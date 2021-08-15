using DataWF.Common;
using System.Collections;

namespace DataWF.Common
{
    public interface IVirtualList : IList
    {
        IModelView ModelView { get; set; }

        int PageSize { get; set; }
    }
}