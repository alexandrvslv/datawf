using DataWF;
using DataWF.Data;
using System.Collections.Generic;

namespace DataWF.WebService.Common
{
    [Invoker]
    public partial class PageContent<T> where T : DBItem
    {
        public HttpPageSettings Info { get; set; }

        public IEnumerable<T> Items { get; set; }
    }
}
