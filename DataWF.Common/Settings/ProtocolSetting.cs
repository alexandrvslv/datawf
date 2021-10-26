using System.Collections.Generic;

namespace DataWF.Common
{
    public class ProtocolSetting
    {
        public static ProtocolSetting Current { get; private set; }

        public ProtocolSetting()
        {
            Current = this;
        }

        public string Protocol { get; set; } = "datawf";

        public string Host { get; set; } = "default.net";
    }
}