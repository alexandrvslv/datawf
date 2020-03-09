using System.Collections.Generic;

namespace DataWF.Common
{
    public class WebNotifyMessage
    {
        public string From { get; set; }

        public List<string> To { get; set; }

        public string Data { get; set; }

    }
}