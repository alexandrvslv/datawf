using DataWF.Invokers;
using System.Collections.Generic;

namespace DataWF
{
    [Invoker]
    public partial class WebNotifyMessage
    {
        public string Header { get; set; }

        public string Data { get; set; }

    }
}