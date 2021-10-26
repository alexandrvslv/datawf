namespace DataWF.Common
{
    public class LDAPSetting
    {
        public static LDAPSetting Current { get; private set; }
        public LDAPSetting()
        {
            Current = this;
        }
        public string Host { get; set; }
        public int? Port { get; set; }
        public string Domain { get; set; }
        public bool SSL { get; set; }
        public string DefaultUser { get; set; }
        public string DefaultPassword { get; set; }
    }
}
