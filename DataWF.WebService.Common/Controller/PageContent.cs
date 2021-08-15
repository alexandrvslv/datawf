using DataWF.Common;
using DataWF.Data;
using System.Collections.Generic;

namespace DataWF.WebService.Common
{
    [InvokerGenerator]
    public partial class PageContent<T> where T : DBItem
    {
        public HttpPageSettings Info { get; set; }

        public IEnumerable<T> Items { get; set; }
    }
}
