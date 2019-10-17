using DataWF.Common;
using System.Collections.Generic;

namespace DataWF.Web.Common
{
    public class AccessUpdatePackage
    {
        public List<string> Ids { get; set; }
        public List<AccessItem> Items { get; set; }
    }
}
