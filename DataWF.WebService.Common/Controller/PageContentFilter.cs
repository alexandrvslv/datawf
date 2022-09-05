using DataWF.WebClient.Common;
using System.Collections.Generic;

namespace DataWF.WebService.Common
{
    public class PageContentFilter
    {
        public HttpPageSettings Info { get; set; }

        public List<object> Items { get; set; }
        //public string Property { get; set; }
    }
}
