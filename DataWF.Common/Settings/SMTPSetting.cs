using System.IO;

namespace DataWF.Common
{
    public class SMTPSetting
    {
        public static SMTPSetting Current { get; private set; }

        public SMTPSetting()
        {
            Current = this;
        }

        public string Host { get; set; }
        public int Port { get; set; }
        public bool SSL { get; set; }
        public string DefaultEmail { get; set; }
        public string DefaultPassword { get; set; }
        public string PassKey { get; set; }
    }
}
