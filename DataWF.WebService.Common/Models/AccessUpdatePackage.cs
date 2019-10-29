using DataWF.Common;
using System.Collections.Generic;

namespace DataWF.WebService.Common
{
    public class AccessUpdatePackage
    {
        public List<string> Ids { get; set; }
        public List<AccessItem> Items { get; set; }
    }
}
