namespace DataWF.Common
{
    public class NetStatEntry
    {
        internal int count;
        internal long length;

        public string Name { get; set; }

        public int Count { get => count; }

        [DefaultFormat("size")]
        public long Length { get => length; }

        public class NameInvoker : Invoker<NetStatEntry, string>
        {
            public static readonly NameInvoker Instance = new NameInvoker();
            public override string Name => nameof(Name);

            public override bool CanWrite => true;

            public override string GetValue(NetStatEntry target) => target.Name;

            public override void SetValue(NetStatEntry target, string value) => target.Name = value;
        }
    }
}
