using DataWF.Invokers;

namespace DataWF
{
    public class NetStatEntry
    {
        public static readonly NameInvoker NameInvokerInstance = new();

        internal int count;
        internal long length;

        public string Name { get; set; }

        public int Count => count;

        [DefaultFormat("size")]
        public long Length => length;

        public class NameInvoker : Invoker<NetStatEntry, string>
        {
            public override string Name => nameof(Name);

            public override bool CanWrite => true;

            public override string GetValue(NetStatEntry target) => target.Name;

            public override void SetValue(NetStatEntry target, string value) => target.Name = value;
        }
    }
}
