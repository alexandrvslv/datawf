using DataWF.Data;
using DataWF.WebClient.Common;
using System.Collections.Generic;

namespace DataWF.WebService.Common
{
    public class PageContent<T> where T : DBItem
    {
        public HttpPageSettings Info { get; set; }

        public IEnumerable<T> Items { get; set; }
    }
}
